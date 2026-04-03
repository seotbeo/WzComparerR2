using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.MapRender.Patches2;

namespace WzComparerR2.MapRender
{
    public class BehaviorController
    {
        public BehaviorController(LifeItem life, FootholdManager fhManager, bool movementEnabled, bool summoned = false, bool playRegenMotion = true)
        {
            this.MovementEnabled = movementEnabled;
            this.Summoned = summoned;
            this.PlayRegenMotion = playRegenMotion;
            this.bState = BaseState.None;
            this.hState = HorizontalState.Stop;
            this.vState = VerticalState.Stop;
            this.pState = ProvokeState.None;
            this.fState = FlyingState.Start;

            this.Owner = life;
            this.ID = life.ID;

            this.FHManager = fhManager;

            this.x = life.X;
            this.y = life.Y;
            this.rx0 = life.Rx0;
            this.rx1 = life.Rx1;
            this.cy = life.Cy;
            this.basePos = new Vector2(this.x, this.cy);
            this.relPos = Vector2.Zero;
            this.baseFlipX = life.Flip;
            this.FlipX = this.baseFlipX;
            this.baseFoothold = life.Fh;
            this.baseFootholdGroup = FHManager.GetGroupIndexByFootholdIndex(this.baseFoothold);
            this.availableArea = this.FHManager.Area;

            InitCurFoothold(this.basePos);
            InitMoveXLimit();

            this.curLayer = FHManager.GetLayerByFoothold(this.curFoothold);
            this.curLayerFoothold = this.curFoothold;
        }

        #region Consts
        private const int Walk_Force = 1400;
        private const int Walk_Drag = 800;
        private const int SpeedBase = 125;
        private const int Fly_SpeedBase = 200;
        private const int Fly_Force = 1200;
        private const float Fly_Dec = 0.35f;
        private const int Max_FallSpeed = 670;
        private const int JumpSpeed = 555;
        private const int GravityAcc = 2000;
        private const float RandomJumpProb = 0.003f;
        private const int JumpTriggerDistanceRange = 7;
        private const int DamageBase = 70;
        private const int DamageRange = 50;
        private const float RandomRestTimeBase = 2.5f;
        private const float RandomRestTimeRange = 2f;
        private const int PosGuardMargin = 500;
        #endregion

        #region Inner States
        private InitState inited = InitState.None;
        private BaseState bState;
        private HorizontalState hState;
        private VerticalState vState;
        private ProvokeState pState;

        private float hSpeed = 0;
        private float vSpeed = 0;
        private int hp = 100;

        private TimeSpan restTime;

        private int curLayer = -1;
        private int curFootholdGroupID = -1;
        private FootholdItem curLayerFoothold;
        private FootholdItem curFoothold;
        private Vector2 relPos;

        private int attackIdx = -1;
        private int attackType = -1;

        private FlyingState fState;
        private TimeSpan flyingPhaseTime;
        private Vector2 fly_TargetPos;
        private FootholdItem fly_TargetFoothold;
        private bool finishFlyX;
        private bool finishFlyY;
        private int fly_ToTargetDirX;
        private int fly_ToTargetDirY;

        private bool HasMoveTarget; // 이동 목표까지 상태 결정 차단
        #endregion

        private ReadOnlyCollection<string> aniList;
        private ReadOnlyCollection<string> attackList;
        private ReadOnlyCollection<string> skillList;

        private readonly TimeSpan MaxFrameRate = TimeSpan.FromMilliseconds(33);
        private readonly FootholdManager FHManager;
        private IRandom Random;

        private readonly int baseFoothold;
        private readonly int baseFootholdGroup;

        private readonly int x;
        private readonly int y;
        private readonly int cy;
        private readonly int rx0;
        private readonly int rx1;
        private readonly Rectangle availableArea;

        private Vector2 basePos;
        private bool baseFlipX;

        private int minMovePosX;
        private int maxMovePosX;

        private int speed;
        private int flySpeed;
        private int chaseSpeed;

        private bool hasMoveMotion;
        private bool hasJumpMotion;
        private bool hasFlyMotion;
        private bool hasChaseMotion;
        private bool hasAttackMotion;
        private bool hasSkillMotion;

        private int finalSpeed => this.flying ? this.flySpeed : (this.chasing ? this.chaseSpeed : this.speed);
        private int jumpTriggerDistance => (int)(this.finalSpeed * 0.28f);
        private bool grounded => this.vState == VerticalState.Stop;
        private bool flying => this.vState == VerticalState.Fly;
        private bool flyingToTarget => this.fly_TargetFoothold != null;
        private bool floating => this.vState == VerticalState.Jump || this.vState == VerticalState.Fall;
        private bool jumping => this.vState == VerticalState.Jump;
        private bool falling => this.vState == VerticalState.Fall;
        private bool chasing => this.pState == ProvokeState.Chase;
        private int curFootholdID => this.curFoothold?.ID ?? -1;
        private int curLayerFootholdID => this.curLayerFoothold?.ID ?? -1;

        public LifeItem Owner { get; }
        public int ID { get; }
        public bool NoRegen => this.Summoned;
        public bool Summoned { get; }
        public bool PlayRegenMotion { get; }
        public bool PlayRegenSound { get; set; } = true;
        public bool FlipX { get; private set; }
        public bool MovementEnabled { get; set; }
        public bool Fixed { get; set; }
        public bool BlockRevive { get; set; }
        public bool ForceMoveStop => this.bState == BaseState.Hit || this.bState == BaseState.Died || this.bState == BaseState.Attack;
        public bool CanMove => this.hasMoveMotion;
        public bool CanJump => this.hasJumpMotion && this.bState == BaseState.Idle;
        public bool CanFly => this.hasFlyMotion;
        public bool CanChase => this.hasChaseMotion;
        public bool CanAttack => (this.hasAttackMotion || this.hasSkillMotion) && this.bState == BaseState.Idle;
        public bool CanHit => this.bState == BaseState.Idle || this.bState == BaseState.Hit;
        public int CurFoothold => this.curFootholdID;
        public int CurLayerFoothold => this.curLayerFootholdID;
        public Vector2 RelPos => relPos;
        public Vector2 CurPos => basePos + relPos;
        public Vector2 IntRelPos => new Vector2((int)relPos.X, (int)relPos.Y);
        public Vector2 IntCurPos => basePos + IntRelPos;
        public BaseState BState => this.bState;
        public HorizontalState HState => this.hState;
        public VerticalState VState => this.vState;
        public ProvokeState PState => this.pState;
        public string SelectedAttack
        {
            get
            {
                switch (attackType)
                {
                    case 0:
                        if (this.attackIdx >= 0 && this.attackIdx < this.attackList.Count)
                        {
                            return this.attackList[attackIdx];
                        }
                        break;

                    case 1:
                        if (this.attackIdx >= 0 && this.attackIdx < this.skillList.Count)
                        {
                            return this.skillList[attackIdx];
                        }
                        break;
                }
                return "";
            }
        }

        public void InitRandom(IRandom random)
        {
            this.Random = random;
            this.inited |= InitState.Random;
        }

        public void SetAnimationList(ReadOnlyCollection<string> ani)
        {
            this.aniList = ani;
            this.hasMoveMotion = aniList.Contains("move");
            this.hasJumpMotion = aniList.Contains("jump");
            this.hasFlyMotion = aniList.Contains("fly");
            this.hasChaseMotion = aniList.Contains("chase");
            this.attackList = aniList.Where(a => a.StartsWith("attack")).ToList().AsReadOnly();
            this.skillList = aniList.Where(a => a.StartsWith("skill")).ToList().AsReadOnly();
            this.hasAttackMotion = attackList.Count > 0;
            this.hasSkillMotion = skillList.Count > 0;
            this.inited |= InitState.AnimationList;
        }

        public void SetSpeed(int speed, int flySpeed, int chaseSpeed)
        {
            this.speed = Math.Min((int)(SpeedBase * (100f + speed) / 100f), 200);
            this.flySpeed = (int)(Fly_SpeedBase * (100f + flySpeed) / 100f);
            this.chaseSpeed = Math.Min((int)(SpeedBase * (100f + chaseSpeed) / 100f), 200);
            this.inited |= InitState.Speed;
        }

        public void InitFlyState()
        {
            if (this.CanFly)
            {
                this.basePos.Y = this.y;
                SetHorizontalState(HorizontalState.MoveL);
                SetVerticalState(VerticalState.Fly);
                if (this.Summoned)
                {
                    this.minMovePosX = this.availableArea.Left;
                    this.maxMovePosX = this.availableArea.Right;
                }
            }
            this.inited |= InitState.FlyState;
        }

        public void Reset()
        {
            SetBaseState(BaseState.Regen);
            if (this.flying)
            {
                SetHorizontalState(this.baseFlipX ? HorizontalState.MoveR : HorizontalState.MoveL);
                SetVerticalState(VerticalState.Fly);
            }
            else
            {
                SetHorizontalState(HorizontalState.Stop);
                SetVerticalState(VerticalState.Stop);
            }
            SetProvokeState(ProvokeState.None);
            this.HasMoveTarget = false;
            this.fly_TargetFoothold = null;
            this.relPos = Vector2.Zero;
            this.flyingPhaseTime = TimeSpan.Zero;
            this.fState = FlyingState.Start;
            this.finishFlyX = false;
            this.finishFlyY = false;
            this.hSpeed = 0;
            this.vSpeed = 0;
            this.FlipX = this.baseFlipX;
            InitCurFoothold(this.basePos);
            this.hp = 100;
        }

        public void SetRegen()
        {
            SetBaseState(BaseState.Regen);
        }

        public void SetBaseIdle()
        {
            SetBaseState(BaseState.Idle);
        }

        public void SetHit()
        {
            SetBaseState(BaseState.Hit);
        }

        public void SetDied(bool blockRevive = false)
        {
            this.BlockRevive = blockRevive;
            SetBaseState(BaseState.Died);
        }

        public void SetAttack()
        {
            if (this.attackIdx < 0 || this.attackType < 0) return;

            SetBaseState(BaseState.Attack);
        }

        public void SetChase()
        {
            if (this.CanChase && !this.chasing)
            {
                this.restTime = TimeSpan.Zero; // stand 모션 취소
                SetProvokeState(ProvokeState.Chase);
            }
        }

        public void DoDamage(int? dam = null)
        {
            this.hp -= dam ?? this.Random.NextVar(DamageBase, DamageRange, true);
        }

        public bool DecideDie()
        {
            return this.hp <= 0;
        }

        public string DecideAttack()
        {
            if (this.hasAttackMotion || this.hasSkillMotion)
            {
                if (this.Random.NextPercent((float)attackList.Count / (attackList.Count + skillList.Count)))
                {
                    this.attackIdx = this.Random.Next(attackList.Count);
                    this.attackType = 0;
                }
                else
                {
                    this.attackIdx = this.Random.Next(skillList.Count);
                    this.attackType = 1;
                }
            }
            return SelectedAttack;
        }

        public void RecoverHit()
        {
            SetBaseState(BaseState.Idle);
            SetChase();
        }

        public void RecoverDied()
        {
            var prev = this.hState;
            Reset();
            SetHorizontalState(this.flying ? prev : HorizontalState.Stop);
            this.PlayRegenSound = true; // 리젠 소리 제한 해제
        }

        public void EndAttack()
        {
            SetBaseState(BaseState.Idle);
        }

        public void EndRegen()
        {
            SetBaseState(BaseState.Idle);
        }

        public void Update(TimeSpan elapsedTime)
        {
            if (this.inited != InitState.All)
            {
                return;
            }

            if (this.bState == BaseState.Regen || this.bState == BaseState.Died) return;
            if (elapsedTime > MaxFrameRate) elapsedTime = MaxFrameRate;

            if (this.MovementEnabled && !this.Fixed)
            {
                var prevPos = CurPos;
                Move(elapsedTime, prevPos);
            }

            if (this.flying && this.finishFlyY)
            {
                this.flyingPhaseTime += elapsedTime;
                return;
            }
            if (this.ForceMoveStop || this.HasMoveTarget) return;

            this.restTime -= elapsedTime;
            if (this.restTime <= TimeSpan.Zero && !this.floating)
            {
                DecideState(CurPos);
                this.restTime = TimeSpan.FromSeconds(this.Random.NextVar(RandomRestTimeBase, RandomRestTimeRange, true));
            }
        }

        private void DecideState(Vector2 pos)
        {
            var coef = this.Random.Next(4);
            if (this.grounded)
            {
                if (this.chasing)
                {
                    switch (coef)
                    {
                        case 0:
                        case 1:
                            SetHorizontalState(HorizontalState.MoveL);
                            this.FlipX = false;
                            break;

                        case 2:
                        case 3:
                            SetHorizontalState(HorizontalState.MoveR);
                            this.FlipX = true;
                            break;
                    }
                }
                else if (this.CanMove)
                {
                    switch (coef)
                    {
                        case 0:
                            SetHorizontalState(HorizontalState.MoveL);
                            this.FlipX = false;
                            break;

                        case 1:
                            SetHorizontalState(HorizontalState.MoveR);
                            this.FlipX = true;
                            break;

                        case 2:
                        case 3:
                            SetHorizontalState(HorizontalState.Stop);
                            break;
                    }
                }
            }
        }

        private void Move(TimeSpan elapsedTime, Vector2 prevPos)
        {
            if (this.flying)
            {
                FlyToTarget(elapsedTime, prevPos);
            }
            else
            {
                MoveX(elapsedTime, prevPos);
                MoveY(elapsedTime, prevPos);
                DecideJump(CurPos);
                RandomJump();
            }
            ExecuteOthers();
            PosGuard();
        }

        private void MoveX(TimeSpan elapsedTime, Vector2 prevPos)
        {
            if (this.hState != HorizontalState.Stop && !this.ForceMoveStop && !this.flyingToTarget)
            {
                var dir = this.hState == HorizontalState.MoveL ? -1 : 1;
                /* TODO: hspeed 감/가속 반영
                this.hSpeed += dir * Walk_Force * (float)elapsedTime.TotalSeconds;
                this.hSpeed = MathHelper.Clamp(this.hSpeed, -this.finalSpeed, this.finalSpeed);
                var newX = this.relPos.X + this.hSpeed * (float)elapsedTime.TotalSeconds;
                this.relPos.X += newX;
                */
                var newX = this.relPos.X + dir * Math.Max(0, this.finalSpeed) * (float)elapsedTime.TotalSeconds;

                var isOutOfRange = IsEndOfAvailableRange(basePos.X + newX);
                if (isOutOfRange) // 가능 범위 밖이면 무조건 flip
                {
                    if (this.floating) // 점프 중에는 X이동 정지
                    {
                        SetHorizontalState(HorizontalState.Stop);
                        this.restTime = TimeSpan.Zero; // 착지 후 바로 다음 행동 결정
                        //this.hSpeed = 0;
                        return;
                    }
                    else
                    {
                        DoFlipX();
                        //this.hSpeed = 0;
                        return;
                    }
                }

                var curfh = this.curFoothold;
                var nextFH = GetNextFoothold(dir, basePos.X + newX);
                var IsEndOfCurFoothold = nextFH == null;
                if (IsEndOfCurFoothold && curfh != null) // 이어진 발판 끝인 경우
                {
                    var canJumpOrFall = this.CanJump;
                    if (this.flying || !canJumpOrFall)
                    {
                        DoFlipX();
                        //this.hSpeed = 0;
                        return;
                    }
                    else
                    {
                        if (IsLastFoothold(curfh, dir)) // 마지막 발판인 경우 점프 가능 else 낙하 가능
                        {
                            canJumpOrFall = canJumpOrFall && HasPossibleFoothold(new Vector2(basePos.X + newX, this.CurPos.Y), dir, 1000, -80, 1500, -JumpSpeed); // 점프 가능 발판 있는지 탐색
                            if (canJumpOrFall)
                            {
                                DoJump();
                                this.relPos.X = newX;
                                return;
                            }
                            else
                            {
                                DoFlipX();
                                //this.hSpeed = 0;
                                return;
                            }
                        }
                        else
                        {
                            if (canJumpOrFall) // 낙하는 아래쪽으로만
                            {
                                var nextfh = dir < 0 ? curfh.PrevFH : curfh.NextFH;
                                if (nextfh != null)
                                {
                                    var startPosY = dir < 0 ? curfh.Y1 : curfh.Y2;
                                    var endPosY = dir < 0 ? nextfh.Y1 : nextfh.Y2;
                                    if (startPosY <= endPosY) canJumpOrFall = true;
                                    else canJumpOrFall = false;
                                }
                                else canJumpOrFall = false;
                            }
                            canJumpOrFall = canJumpOrFall && HasPossibleFoothold(new Vector2(basePos.X + newX, this.CurPos.Y), dir, 1000, 0, 1500, 0); // 낙하 가능 발판 있는지 탐색
                            if (canJumpOrFall)
                            {
                                DoFall();
                                this.relPos.X = newX;
                                return;
                            }
                            else
                            {
                                DoFlipX();
                                //this.hSpeed = 0;
                                return;
                            }
                        }
                    }
                }
                else // 이어진 발판 내 이동
                {
                    if (nextFH != this.curFoothold)
                    {
                        SetCurFoothold(nextFH);
                    }
                    this.relPos.X = newX;
                    return;
                }
            }
            else if (this.hState == HorizontalState.Stop) // TODO: 감속
            {
                /*
                if (this.hSpeed < 0)
                {
                    this.hSpeed += Walk_Drag * (float)elapsedTime.TotalSeconds;
                    this.hSpeed = Math.Min(this.hSpeed, 0);
                    var newX = this.hSpeed * (float)elapsedTime.TotalSeconds;
                    this.relPos.X += newX;
                }
                else if (this.hSpeed > 0)
                {
                    this.hSpeed -= Walk_Drag * (float)elapsedTime.TotalSeconds;
                    this.hSpeed = Math.Max(this.hSpeed, 0);
                    var newX = this.hSpeed * (float)elapsedTime.TotalSeconds;
                    this.relPos.X += newX;
                }
                */
            }
        }

        private void MoveY(TimeSpan elapsedTime, Vector2 prevPos)
        {
            if (this.grounded && !this.ForceMoveStop) // 발판에 스냅
            {
                var newY = GetRelYOnFoothold(CurPos.X, CurPos.Y);
                this.relPos.Y += newY;
            }
            else if (this.floating)
            {
                this.vSpeed += GravityAcc * (float)elapsedTime.TotalSeconds;
                this.vSpeed = Math.Min(this.vSpeed, Max_FallSpeed);
                var newY = this.vSpeed * (float)elapsedTime.TotalSeconds;
                this.relPos.Y += newY;
                if (this.vSpeed > 0)
                {
                    SetVerticalState(VerticalState.Fall);
                }

                CollisionTest(prevPos, CurPos); // 착지 시 curFoothold != -1

                if (this.falling)
                {
                    var collisionOn = GetRelYOnFoothold(CurPos.X, basePos.Y); // curFoothold 기준으로 y축 상대 좌표
                    if (this.relPos.Y >= collisionOn) // 착지 판정
                    {
                        this.relPos.Y = collisionOn;
                        this.vSpeed = 0;
                        this.HasMoveTarget = false;
                        SetVerticalState(VerticalState.Stop);
                    }
                }
            }
        }

        private void FlyToTarget(TimeSpan elapsedTime, Vector2 prevPos)
        {
            if (this.ForceMoveStop) return;

            if (this.fState == FlyingState.Start) // 첫 소환 시; 같은 그룹/모든 그룹 50% 확률
            {
                SetFlyTarget(prevPos, 0.5f, differentGroup: false);
            }
            else if (this.fState == FlyingState.Idle) // 이동 완료되면 다음 타겟 탐색; 1% 확률로 다른 그룹으로
            {
                SetFlyTarget(prevPos, 0.99f, differentGroup: true);
            }

            if (this.fState == FlyingState.NoTarget) // 타겟 찾기 실패
            {
                if (this.Random.NextPercent(0.97f)) // 97% 확률로 제자리 통통 튐
                {
                    this.vSpeed += GravityAcc * Fly_Dec * (float)elapsedTime.TotalSeconds;
                    this.vSpeed = Math.Min(this.vSpeed, Max_FallSpeed * Fly_Dec);
                    var newY = this.vSpeed * (float)elapsedTime.TotalSeconds;

                    this.relPos.Y += newY;
                    if (this.CurPos.Y >= this.fly_TargetPos.Y)
                    {
                        this.vSpeed = -Math.Min(JumpSpeed * Fly_Dec, this.finalSpeed);
                        this.relPos.Y = (this.fly_TargetPos - this.basePos).Y;
                    }
                }
                else
                {
                    SetFlyTarget(prevPos, 0f, differentGroup: false); // 3% 확률로 모든 발판 그룹에서 재탐색
                }
            }

            if (this.fState == FlyingState.FlyToTarget) // 타겟 존재
            {
                if (!this.finishFlyX) // X축 이동거리 남았을 때, 이동
                {
                    var dir = this.fly_ToTargetDirX;
                    this.hSpeed += dir * Fly_Force * (float)elapsedTime.TotalSeconds;
                    this.hSpeed = MathHelper.Clamp(this.hSpeed, -this.finalSpeed, this.finalSpeed);
                    var newX = this.relPos.X + this.hSpeed * (float)elapsedTime.TotalSeconds;
                    
                    if ((dir < 0 && this.basePos.X + newX <= this.fly_TargetPos.X) ||
                            (dir > 0 && this.basePos.X + newX >= this.fly_TargetPos.X))
                    {
                        this.finishFlyX = true;
                        this.relPos.X = (this.fly_TargetPos - this.basePos).X;
                    }
                    else
                    {
                        this.relPos.X = newX;
                    }
                }
                else // X축 이동 끝일 때는 좌우 반복 이동
                {
                    var max = Math.Max(0, this.finalSpeed);
                    if (this.hState == HorizontalState.MoveL)
                    {
                        this.FlipX = true;
                        this.hSpeed += Fly_Force * (float)elapsedTime.TotalSeconds;
                        if (this.hSpeed > max)
                        {
                            this.hSpeed = max;
                            SetHorizontalState(HorizontalState.MoveR, invoke: false);
                        }
                    }
                    else if (this.hState == HorizontalState.MoveR)
                    {
                        this.FlipX = false;
                        this.hSpeed += -Fly_Force * (float)elapsedTime.TotalSeconds;
                        if (this.hSpeed < -max)
                        {
                            this.hSpeed = -max;
                            SetHorizontalState(HorizontalState.MoveL, invoke: false);
                        }
                    }
                    var newX = this.hSpeed * (float)elapsedTime.TotalSeconds;
                    this.relPos.X += newX;
                }

                if (!this.finishFlyY) // Y축 이동거리 남았을 때, 이동
                {
                    var dir = this.fly_ToTargetDirY;
                    if ((dir < 0 && this.vSpeed >= 0))
                    {
                        this.vSpeed = dir * JumpSpeed * Fly_Dec;
                    }
                    else
                    {
                        this.vSpeed += dir * GravityAcc * Fly_Dec * (float)elapsedTime.TotalSeconds;
                    }
                    this.vSpeed = MathHelper.Clamp(this.vSpeed, -this.finalSpeed, this.finalSpeed);

                    var newY = this.relPos.Y + this.vSpeed * (float)elapsedTime.TotalSeconds;
                    if ((dir < 0 && this.basePos.Y + newY <= this.fly_TargetPos.Y) ||
                            (dir > 0 && this.basePos.Y + newY >= this.fly_TargetPos.Y))
                    {
                        this.finishFlyY = true;
                        this.vSpeed = -Math.Min(JumpSpeed * Fly_Dec, this.finalSpeed);
                        this.relPos.Y = (this.fly_TargetPos - this.basePos).Y;
                    }
                    else
                    {
                        this.relPos.Y = newY;
                    }
                }
                else // Y축 이동 끝일 때는 통통 튐
                {
                    this.vSpeed += GravityAcc * Fly_Dec * (float)elapsedTime.TotalSeconds;
                    this.vSpeed = Math.Min(this.vSpeed, Max_FallSpeed * Fly_Dec);
                    var newY = this.vSpeed * (float)elapsedTime.TotalSeconds;

                    this.relPos.Y += newY;
                    if (this.CurPos.Y > this.fly_TargetPos.Y)
                    {
                        this.vSpeed = -Math.Min(JumpSpeed * Fly_Dec, this.finalSpeed);
                        this.relPos.Y = (this.fly_TargetPos - this.basePos).Y;
                    }
                }

                if (this.finishFlyX && this.finishFlyY) // 이동 완료
                {
                    EndFlyTarget();
                }
            }
        }

        private void DoFlipX()
        {
            if (this.hState == HorizontalState.MoveL) this.hState = HorizontalState.MoveR;
            else this.hState = HorizontalState.MoveL;
            this.FlipX = !this.FlipX;
        }

        /// <summary>
        /// dir 방향으로 이동할 때, x 위치가 포함된 다음 발판을 반환합니다. 없다면 null을 반환합니다.
        /// </summary>
        /// <param name="dir">왼쪽으로 이동하면 음수, 아니면  양수</param>
        /// <param name="x">x 위치</param>
        /// <returns></returns>
        private FootholdItem GetNextFoothold(int dir, float x)
        {
            FootholdItem fh = this.curFoothold;
            if (fh != null)
            {
                HashSet<int> hs = new HashSet<int>();
                if (dir < 0)
                {
                    while (true)
                    {
                        if (!hs.Add(fh.ID)) return null;
                        if (x >= fh.X1)
                        {
                            return fh;
                        }
                        else
                        {
                            if (fh.PrevFH != null)
                            {
                                if (fh.PrevFH.IsWall)
                                {
                                    return null;
                                }
                                else
                                {
                                    fh = fh.PrevFH;
                                    continue;
                                }
                            }
                            else return null;
                        }
                    }
                }
                else if (dir > 0)
                {
                    while (true)
                    {
                        if (!hs.Add(fh.ID)) return null;
                        if (x <= fh.X2)
                        {
                            return fh;
                        }
                        else
                        {
                            if (fh.NextFH != null)
                            {
                                if (fh.NextFH.IsWall)
                                {
                                    return null;
                                }
                                else
                                {
                                    fh = fh.NextFH;
                                    continue;
                                }
                            }
                            else return null;
                        }
                    }
                }
            }

            return fh;
        }

        /// <summary>
        /// 입력값 x가 minMovePosX와 maxMovePosX 범위를 벗어났는지 확인합니다.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private bool IsEndOfAvailableRange(float x)
        {
            if (x < this.minMovePosX || x > this.maxMovePosX)
                return true;

            return false;
        }

        /// <summary>
        /// dir 방향으로 이동할 때, 현재 발판이 이어진 발판의 끝인지 확인합니다.
        /// </summary>
        /// <param name="fh">현재 발판</param>
        /// <param name="dir">왼쪽으로 이동하면 음수, 아니면  양수</param>
        /// <returns></returns>
        private bool IsLastFoothold(FootholdItem fh, int dir)
        {
            if (dir < 0)
            {
                if (fh.PrevFH == null)
                {
                    return true;
                }
            }
            else if (dir > 0)
            {
                if (fh.NextFH == null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// pos 위치에서 수직 아래 가장 가까운 발판 그룹을 찾습니다. 없으면 수직 위 발판 중 가장 아래에 있는 발판 그룹을 찾습니다. 둘 다 없으면 -1을 반환합니다.
        /// </summary>
        /// <param name="pos">기준 위치</param>
        /// <returns>Foothold Group Index</returns>
        private int VRayCastingTest(Vector2 pos)
        {
            FootholdItem selectedBelow = null;
            FootholdItem selectedUpper = null;
            var belowY = int.MaxValue;
            var upperY = int.MinValue;
            foreach (var group in FHManager.AllFootholdGroups.Where(g => pos.X >= g.GroupArea.Left && pos.X <= g.GroupArea.Right))
            {
                foreach (var fh in group.Footholds.Where(fh => pos.X >= fh.FootholdArea.Left && pos.X <= fh.FootholdArea.Right && !fh.IsWall).Select(fh =>
                {
                    return new
                    {
                        Foothold = fh,
                        Y = FHManager.GetYOnFoothold(fh, pos.X)
                    };
                }))
                {
                    if (fh.Y < belowY && fh.Y >= pos.Y)
                    {
                        selectedBelow = fh.Foothold;
                        belowY = fh.Y;
                    }
                    else if (fh.Y > upperY && fh.Y <= pos.Y)
                    {
                        selectedUpper = fh.Foothold;
                        upperY = fh.Y;
                    }
                }
            }
            return selectedBelow?.GroupIndex ?? selectedUpper?.GroupIndex ?? -1;
        }

        /// <summary>
        /// pos 위치에서 groupID 발판으로 수직 아래 가장 가까운 발판이 있는지 확인합니다.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="groupID"></param>
        /// <returns></returns>
        private bool VRayCastingTestInCurGroup(Vector2 pos, int groupID)
        {
            FootholdItem selectedBelow = null;
            var belowY = int.MaxValue;
            var group = FHManager.GetGroupByIndex(groupID);
            foreach (var fh in group.Footholds.Where(fh => pos.X >= fh.FootholdArea.Left && pos.X <= fh.FootholdArea.Right && !fh.IsWall).Select(fh =>
            {
                return new
                {
                    Foothold = fh,
                    Y = FHManager.GetYOnFoothold(fh, pos.X)
                };
            }))
            {
                if (fh.Y < belowY && fh.Y >= pos.Y)
                {
                    selectedBelow = fh.Foothold;
                    belowY = fh.Y;
                }
            }
            return (selectedBelow?.GroupIndex ?? -1) != -1;
        }
        
        /// <summary>
        /// 수평 충돌이 발생했는지 확인합니다.
        /// </summary>
        /// <param name="prevPos">이전 위치</param>
        /// <param name="nextPos">다음 위치</param>
        /// <param name="useCurFootholdGroup">curFootholdGroupID에 해당하는 group에서만 수평 충돌을 확인할지 결정합니다.</param>
        /// <returns>수평 충돌 발생 시, 발생한 FootholdItem</returns>
        private FootholdItem HCollisionTest(Vector2 prevPos, Vector2 nextPos, bool useCurFootholdGroup)
        {
            var dir = prevPos.X > nextPos.X ? -1 : 1;
            var ydir = prevPos.Y > nextPos.Y ? -1 : 1; // vSpeed < 0 일때는 Reversed 발판까지 탐지
            int gi = -1;
            if (useCurFootholdGroup) gi = this.curFootholdGroupID;
            else gi = VRayCastingTest(prevPos); // 수직 아래 가장 가까운 발판 그룹 찾기, 없으면 위 발판 중 가장 아래에 있는 발판 그룹

            if (gi != -1)
            {
                var group = FHManager.GetGroupByIndex(gi);
                if (group != null)
                {
                    foreach (var fh in group.Footholds.Where(f => (ydir < 0 ? f.IsWall : f.Vertical) && (dir < 0 ? f.Y1 <= f.Y2 : f.Y1 >= f.Y2) &&
                        FootholdManager.GetCandidateFootholds(f, prevPos, nextPos)))
                    {
                        if (FootholdManager.Intersects(fh, prevPos, nextPos))
                        {
                            return fh;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 수평 또는 수직 충돌이 발생했는지 확인하고, 발생 시 이벤트를 처리합니다.
        /// </summary>
        /// <param name="prevPos">이전 위치</param>
        /// <param name="nextPos">다음 위치</param>
        /// <returns>수직 충돌의 발생 여부</returns>
        private bool CollisionTest(Vector2 prevPos, Vector2 nextPos)
        {
            if (prevPos.Y == nextPos.Y) return false;

            var dir = prevPos.X > nextPos.X ? -1 : 1;
            // 수직 발판과 충돌 체크
            // 현재 발판 그룹에서 벗어난 경우, 발판 그룹을 재탐색, 아니면 현재 발판 그룹의 수직 발판만 대상
            var useCurFootholdGroup = VRayCastingTestInCurGroup(prevPos, this.curFootholdGroupID);
            FootholdItem hfh;
            if ((hfh = HCollisionTest(prevPos, nextPos, useCurFootholdGroup: useCurFootholdGroup)) != null)
            {
                HandleHCollision(hfh, prevPos, ref nextPos); // 수평 충돌시 정지
            }

            // 낙하 중에만 착지 체크
            if (this.falling)
            {
                foreach (var group in FHManager.AllFootholdGroups.Where(g => FootholdManager.GetCandidateGroups(g, prevPos, nextPos)))
                {
                    foreach (var fh in group.Footholds.Where(f => !f.IsWall && FootholdManager.GetCandidateFootholds(f, prevPos, nextPos)))
                    {
                        if (FootholdManager.Intersects(fh, prevPos, nextPos))
                        {
                            HandleFallVCollision(fh); // 착지한 발판으로 curFoothold와 curFootholdGroup 변경
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 낙하 시, 수직 충돌한 발판으로 curFoothold와 curFootholdGroup을 변경합니다.
        /// </summary>
        /// <param name="fh">충돌한 발판</param>
        private void HandleFallVCollision(FootholdItem fh)
        {
            if (fh.IsWall) return;

            SetCurFoothold(fh);
        }

        /// <summary>
        /// 수평 충돌한 발판 기준으로 수평 이동을 정지합니다. nextPos 값이 보정됩니다.
        /// </summary>
        /// <param name="fh">충돌한 발판</param>
        /// <param name="prevPos">이전 위치</param>
        /// <param name="nextPos">다음 위치</param>
        private void HandleHCollision(FootholdItem fh, Vector2 prevPos, ref Vector2 nextPos)
        {
            if (!fh.IsWall) return;

            SetHorizontalState(HorizontalState.Stop);
            //this.hSpeed = 0;
            this.relPos.X = prevPos.X - basePos.X;
            nextPos.X = prevPos.X;
            if (fh.Reversed)
            {
                nextPos.Y = FHManager.GetYOnFoothold(fh, nextPos.X);
                this.vSpeed = 0;
            }
        }

        /// <summary>
        /// curFoothold에서 x 위치의 높이값과 y의 차이를 반환합니다.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int GetRelYOnFoothold(float x, float y)
        {
            var fh = this.curFoothold;
            if (fh != null && !fh.Vertical)
            {
                return (int)(FHManager.GetYOnFoothold(fh, x) - y);
            }
            return int.MaxValue;
        }

        private void DecideJump(Vector2 pos)
        {
            if (!this.grounded || !this.CanJump || this.HasMoveTarget || this.hState == HorizontalState.Stop) return;

            bool canJump = false;
            int x = (int)pos.X;
            int dir = this.hState == HorizontalState.MoveL ? -1 : 1;
            var fh = this.curFoothold;
            if ((x >= this.minMovePosX || dir > 0) &&
                (x <= this.maxMovePosX || dir < 0) &&
                fh != null)
            {
                FootholdItem nextfh = fh;
                FootholdItem endfh = fh;
                Vector2 fallPos = pos;
                var next = dir < 0 ? fh.Prev : fh.Next;

                var minJumpTriggerDistance = this.jumpTriggerDistance - JumpTriggerDistanceRange;
                var maxJumpTriggerDistance = this.jumpTriggerDistance + JumpTriggerDistanceRange;

                HashSet<int> hs = new HashSet<int>();
                while (true) // 발판 끝부터 거리 확인
                {
                    if (!hs.Add(next)) return;
                    if (next == 0)
                    {
                        endfh = nextfh;
                        var d = Math.Abs((dir < 0 ? nextfh.X1 : nextfh.X2) - x);
                        if (!(d <= maxJumpTriggerDistance && d >= minJumpTriggerDistance))
                        {
                            return;
                        }
                        else break;
                    }
                    else if ((nextfh = dir < 0 ? nextfh.PrevFH : nextfh.NextFH) != null)
                    {
                        if (nextfh.IsWall)
                        {
                            var d = Math.Abs((dir < 0 ? nextfh.X1 : nextfh.X2) - x);
                            if (d <= maxJumpTriggerDistance && d >= minJumpTriggerDistance)
                            {
                                endfh = nextfh;
                                break;
                            }
                            else return;
                        }
                        else
                        {
                            next = dir < 0 ? nextfh.Prev : nextfh.Next;
                            continue;
                        }
                    }
                    else return;
                }

                var limitX1 = dir < 0 ? Math.Max(Math.Min(0, this.minMovePosX - x), -100) : Math.Min(Math.Max(0, this.maxMovePosX - x), 100);
                canJump = HasPossibleFoothold(new Vector2(x, this.CurPos.Y), dir, Math.Abs(limitX1), -80, 0, -JumpSpeed, sameGroup: true); // 점프 가능 발판 있는지 탐색
                if (canJump)
                {
                    DoJump();
                    this.HasMoveTarget = true; // 목표까지 상태 결정 차단
                }
            }
        }

        public bool HasPossibleFoothold(Vector2 pos, int dir, int limitX, int minLimitY, int maxLimitY, float startVSpeed = 0f, bool sameGroup = false)
        {
            if (limitX == 0 || maxLimitY - minLimitY == 0) return false;

            var minY = pos.Y + minLimitY;
            var maxY = pos.Y + maxLimitY;
            var minX = dir < 0 ? pos.X - limitX : pos.X;
            var maxX = dir < 0 ? pos.X : pos.X + limitX;

            var hSpeed = this.finalSpeed;
            var vSpeed = startVSpeed;
            var dt = 1 / 30f; // 30프레임으로 이동경로 시뮬레이션
            var timestamps = Math.Max(1, Math.Max((int)Math.Ceiling(limitX / Math.Max(1f, hSpeed * dt)), (int)Math.Ceiling((maxLimitY - minLimitY) / Math.Max(1f, Max_FallSpeed * dt))));
            var hPos = pos.X;
            var vPos = pos.Y;
            var prevPos = pos;
            var nextPos = pos;

            var candidateGroups = FHManager.AllFootholdGroups.Where(g => (sameGroup ? g.Index == this.curFootholdGroupID : true) && FootholdManager.GetCandidateGroups(g, new Vector2(minX, minY), new Vector2(maxX, maxY))).ToList();
            for (int i = 0; i < timestamps; i++)
            {
                prevPos = nextPos;
                hPos += dir * hSpeed * (float)dt;
                hPos = Math.Min(Math.Max(hPos, this.minMovePosX), this.maxMovePosX);
                vSpeed += GravityAcc * (float)dt;
                vSpeed = Math.Min(vSpeed, Max_FallSpeed);
                vPos += vSpeed * (float)dt;
                nextPos = new Vector2(hPos, vPos);

                if (HCollisionTest(prevPos, nextPos, useCurFootholdGroup: true) != null) break;
                if (vSpeed < 0) continue;
                if ((nextPos.X <= minX) || (nextPos.X >= maxX)) break;
                if ((nextPos.Y <= minY) || (nextPos.Y >= maxY)) break;

                foreach (var group in candidateGroups)
                {
                    foreach (var fh in group.Footholds)
                    {
                        if (!fh.IsWall && FootholdManager.Intersects(fh, prevPos, nextPos))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void RandomJump()
        {
            if (!this.HasMoveTarget && this.CanJump && this.grounded)
            {
                if (this.Random.NextPercent(RandomJumpProb)) // RandomJumpProb 확률로 점프
                {
                    DoJump();
                }
            }
        }

        private void DoJump()
        {
            this.vSpeed = -JumpSpeed;
            this.curFoothold = null;
            SetVerticalState(VerticalState.Jump);
        }

        private void DoFall()
        {
            this.curFoothold = null;
            SetVerticalState(VerticalState.Fall);
        }

        /// <summary>
        /// 비행 타겟 위치를 설정합니다.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="sameGroupProb"></param>
        /// <param name="differentGroup"></param>
        private void SetFlyTarget(Vector2 pos, float sameGroupProb, bool differentGroup)
        {
            var dir = this.hState == HorizontalState.MoveL ? -1 : 1;
            List<FootholdItem> candidateFHs = new List<FootholdItem>();
            if (this.Random.NextPercent(sameGroupProb)) // 같은 그룹
            {
                candidateFHs = FHManager.AllFootholdGroups.Where(g => g.Index == this.curFootholdGroupID).SelectMany(g => g.Footholds).Where(f => !f.IsWall).ToList();
            }
            else
            {
                if (differentGroup) // 다른 그룹
                {
                    candidateFHs = FHManager.AllFootholdGroups.Where(g => g.Index != this.curFootholdGroupID).SelectMany(g => g.Footholds).Where(f => !f.IsWall).ToList();
                }
                
                if (!differentGroup || candidateFHs.Count == 0) // 모든 그룹
                {
                    var groupCount = FHManager.AllFootholdGroups.Count;
                    if (groupCount > 0)
                    {
                        var selectedGroup = FHManager.AllFootholdGroups[this.Random.Next(groupCount)];
                        candidateFHs = selectedGroup.Footholds.Where(f => !f.IsWall).ToList();
                    }
                }
            }

            var candidateCount = candidateFHs.Count;
            while (candidateCount > 0)
            {
                var selected = candidateFHs[this.Random.Next(candidateCount)];

                if (selected != null)
                {
                    var pos1 = new Vector2(selected.X1, selected.Y1);
                    var pos2 = new Vector2(selected.X2, selected.Y2);

                    var forbidXMin = pos.X - 15; // x축 +-15px이내로는 이동 제한
                    var forbidXMax = pos.X + 15;

                    var segments = new List<Tuple<float, float>>();
                    if (selected.X1 < forbidXMin)
                        segments.Add(new Tuple<float, float>(selected.X1, Math.Min(selected.X2, forbidXMin)));
                    if (selected.X2 > forbidXMax)
                        segments.Add(new Tuple<float, float>(Math.Max(selected.X1, forbidXMax), selected.X2));

                    if (segments.Count == 0)
                    {
                        candidateFHs.Remove(selected);
                        candidateCount--;
                        continue;
                    }

                    var selectedSegment = segments[this.Random.Next(segments.Count)];
                    var x = this.Random.NextVar((selectedSegment.Item1 + selectedSegment.Item2) / 2f, (selectedSegment.Item2 - selectedSegment.Item1) / 2f);
                    var y = FHManager.GetYOnFoothold(selected, x);

                    y += this.Random.NextVar(-15, 20); // 랜덤 높이 (-35 ~ 5px)

                    var dx = x - pos.X;
                    var dy = y - pos.Y;
                    this.fly_ToTargetDirX = dx < 0 ? -1 : 1;
                    this.fly_ToTargetDirY = dy < 0 ? -1 : 1;
                    if (fly_ToTargetDirX < 0)
                    {
                        this.FlipX = false;
                        SetHorizontalState(HorizontalState.MoveL, invoke: false);
                    }
                    else if (fly_ToTargetDirX > 0)
                    {
                        this.FlipX = true;
                        SetHorizontalState(HorizontalState.MoveR, invoke: false);
                    }

                    this.fly_TargetFoothold = selected;
                    this.fly_TargetPos = new Vector2(x, y);

                    if (this.curFootholdGroupID != FHManager.GetGroupIndexByFoothold(selected)) // 발판 그룹 전환; fly 레이어로 변경 (-1)
                        SetCurFoothold(null);
                    this.HasMoveTarget = true;
                    this.fState = FlyingState.FlyToTarget;
                    return;
                }
            }

            this.fState = FlyingState.NoTarget; // 후보 없음
        }

        /// <summary>
        /// 비행 완료 후 상태를 설정합니다.
        /// </summary>
        private void EndFlyTarget()
        {
            SetCurFoothold(this.fly_TargetFoothold);
            this.fly_TargetFoothold = null;
            this.HasMoveTarget = false;
            this.flyingPhaseTime = TimeSpan.Zero;
            this.fState = FlyingState.Idle;
            this.finishFlyX = false;
            this.finishFlyY = false;
        }

        private void ExecuteOthers()
        {

        }

        /// <summary>
        /// 의도치 않게 맵 밖을 크게 벗어난 경우, 초기화합니다.
        /// </summary>
        private void PosGuard()
        {
            if (this.MovementEnabled)
            {
                var x = CurPos.X;
                var y = CurPos.Y;
                if (x < availableArea.Left - PosGuardMargin || x > availableArea.Right + PosGuardMargin ||
                    y < availableArea.Top - PosGuardMargin || y > availableArea.Bottom + PosGuardMargin)
                {
                    SetDied();
                }
            }
        }

        /// <summary>
        /// curFoothold와 curFootholdGroup을 newFH로 갱신합니다.
        /// </summary>
        /// <param name="newFH">대상 발판</param>
        private void SetCurFoothold(FootholdItem newFH)
        {
            if (this.curFootholdID == (newFH?.ID ?? -1)) return;
            this.curFoothold = newFH;
            this.curFootholdGroupID = newFH?.GroupIndex ?? -1;
            SetLayer(this.curFoothold);
        }

        /// <summary>
        /// 첫 소환 시, pos 위치에서 적절한 발판으로 snap합니다.
        /// </summary>
        /// <param name="pos"></param>
        private void InitCurFoothold(Vector2 pos) // Init: 발판에 snap
        {
            FootholdItem selectedBelow = null;
            FootholdItem selectedUpper = null;
            var belowY = int.MaxValue;
            var upperY = int.MinValue;
            foreach (var group in FHManager.AllFootholdGroups.Where(g => pos.X >= g.GroupArea.Left && pos.X <= g.GroupArea.Right))
            {
                foreach (var fh in group.Footholds.Where(fh => !fh.IsWall && pos.X >= fh.FootholdArea.Left && pos.X <= fh.FootholdArea.Right).Select(fh =>
                {
                    return new
                    {
                        Foothold = fh,
                        Y = FHManager.GetYOnFoothold(fh, pos.X)
                    };
                }))
                {
                    if (fh.Y < belowY && fh.Y >= pos.Y)
                    {
                        selectedBelow = fh.Foothold;
                        belowY = fh.Y;
                    }
                    else if (fh.Y > upperY && fh.Y <= pos.Y)
                    {
                        selectedUpper = fh.Foothold;
                        upperY = fh.Y;
                    }
                }
            }
            if (selectedBelow != null || selectedUpper != null)
            {
                FootholdItem finalSelected = null;
                if (selectedBelow != null && selectedUpper != null)
                    finalSelected = pos.Y - upperY <= 15 ? selectedUpper : selectedBelow;
                else finalSelected = selectedBelow ?? selectedUpper;

                if (finalSelected != null)
                {
                    SetCurFoothold(finalSelected);
                    SetVerticalState(this.flying ? VerticalState.Fly : this.MovementEnabled ? VerticalState.Fall : VerticalState.Stop);
                    this.fly_TargetPos.Y = Math.Max(finalSelected.Y1, finalSelected.Y2);
                    return;
                }
            }

            // 초기 발판 찾기 실패
            this.curFoothold = null;
            this.curFootholdGroupID = -1;
            this.Fixed = true;
        }

        /// <summary>
        /// 이동할 수 있는 x 범위를 설정합니다.
        /// </summary>
        private void InitMoveXLimit()
        {
            // 소환된 몹은 rx0 rx1 정보 없음 -> 현재 발판 그룹 기준으로 이동 제한 설정
            if (this.Summoned)
            {
                this.minMovePosX = FHManager.GetGroupByIndex(this.curFootholdGroupID)?.GroupArea.Left ?? this.availableArea.Left;
                this.maxMovePosX = FHManager.GetGroupByIndex(this.curFootholdGroupID)?.GroupArea.Right ?? this.availableArea.Right;
                return;
            }

            // rx0, rx1 기반 이동 제한
            this.minMovePosX = this.rx0;
            this.maxMovePosX = this.rx1;

            // 첫 소환 위치까지 이동 제한 확장
            if (this.basePos.X < this.minMovePosX)
            {
                this.minMovePosX = (int)this.basePos.X;
            }
            else if (this.basePos.X > this.maxMovePosX)
            {
                this.maxMovePosX = (int)this.basePos.X;
            }

            // 평평한 발판인 경우, 같은 발판 그룹의 flat 끝에서 +-20px까지 이동 제한 확장
            // 확장o : 251010403/life/2, 800010100/life/1000 ...
            // 확장x : 101020100/life/2 ...
            var fh = this.curFoothold;
            if (fh != null && fh.Flat)
            {
                FootholdItem prevfh = fh;
                FootholdItem nextfh = fh;
                int min = fh.FootholdArea.Left + 20;
                int max = fh.FootholdArea.Right - 20;

                HashSet<int> hs = new HashSet<int>();
                while (true)
                {
                    if (!hs.Add(prevfh.ID)) break;
                    if (prevfh.PrevFH != null && prevfh.PrevFH.Flat)
                    {
                        prevfh = prevfh.PrevFH;
                        min = prevfh.FootholdArea.Left + 20;
                    }
                    else break;
                }
                hs.Clear();
                while (true)
                {
                    if (!hs.Add(nextfh.ID)) break;
                    if (nextfh.NextFH != null && nextfh.NextFH.Flat)
                    {
                        nextfh = nextfh.NextFH;
                        max = nextfh.FootholdArea.Right - 20;
                    }
                    else break;
                }

                if (this.minMovePosX > min)
                {
                    this.minMovePosX = min;
                }
                if (this.maxMovePosX < max)
                {
                    this.maxMovePosX = max;
                }
            }
        }

        #region Events
        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<LayerChangedEventArgs> LayerChanged;

        protected virtual void OnStateChanged(StateChangedEventArgs e)
            => StateChanged?.Invoke(this, e);
        protected virtual void OnLayerChanged(LayerChangedEventArgs e)
            => LayerChanged?.Invoke(this, e);

        private void SetBaseState(BaseState state, bool invoke = true)
        {
            if (this.bState == state) return;
            var prev = this.bState;
            this.bState = state;
            if (invoke) OnStateChanged(new StateChangedEventArgs(StateType.Base, prev, this.bState, this.hState, this.vState, this.pState));
        }

        private void SetHorizontalState(HorizontalState state, bool invoke = true)
        {
            if (this.hState == state) return;
            var prev = this.hState;
            this.hState = state;
            if (invoke) OnStateChanged(new StateChangedEventArgs(StateType.Horizontal, prev, this.bState, this.hState, this.vState, this.pState));
        }

        private void SetVerticalState(VerticalState state, bool invoke = true)
        {
            if (this.vState == state) return;
            var prev = this.vState;
            this.vState = state;
            if (invoke) OnStateChanged(new StateChangedEventArgs(StateType.Vertical, prev, this.bState, this.hState, this.vState, this.pState));
        }

        private void SetProvokeState(ProvokeState state, bool invoke = true)
        {
            if (this.pState == state) return;
            var prev = this.pState;
            this.pState = state;
            if (invoke) OnStateChanged(new StateChangedEventArgs(StateType.Provoke, prev, this.bState, this.hState, this.vState, this.pState));
        }

        private void SetLayer(FootholdItem nextfh, bool invoke = true)
        {
            var nextLayer = FHManager.GetLayerByFoothold(nextfh);
            if (this.curLayer == nextLayer) return;
            this.curLayer = nextLayer;
            var prevfh = this.curLayerFootholdID;
            this.curLayerFoothold = nextfh;
            if (invoke) OnLayerChanged(new LayerChangedEventArgs(prevfh, this.curLayerFootholdID));
        }

        public class StateChangedEventArgs : EventArgs
        {
            public StateChangedEventArgs(StateType sType, Enum prev, BaseState bState, HorizontalState hState, VerticalState vState, ProvokeState pState)
            {
                this.StateType = sType;
                this.PrevState = prev;
                this.BState = bState;
                this.HState = hState;
                this.VState = vState;
                this.PState = pState;
            }

            public StateType StateType { get; }
            public Enum PrevState { get; }
            public BaseState BState { get; }
            public HorizontalState HState { get; }
            public VerticalState VState { get; }
            public ProvokeState PState { get; }
        }

        public class LayerChangedEventArgs : EventArgs
        {
            public LayerChangedEventArgs(int prevLayer, int newLayer)
            {
                this.PrevLayer = prevLayer;
                this.NewLayer = newLayer;
            }

            // -1 == Fly
            public int PrevLayer { get; }
            public int NewLayer { get; }
        }
        #endregion

        #region Enums
        public enum BaseState
        {
            None = -1,
            Regen,
            Idle,
            Hit,
            Died,
            Attack,
        }

        public enum HorizontalState
        {
            Stop,
            MoveL,
            MoveR,
        }

        public enum VerticalState
        {
            Stop,
            Jump,
            Fall,
            Fly,
        }

        public enum ProvokeState
        {
            None,
            Chase,
        }

        public enum StateType
        {
            Base,
            Horizontal,
            Vertical,
            Provoke,
        }

        private enum FlyingState
        {
            Start,
            Idle,
            FlyToTarget,
            NoTarget,
        }

        private enum InitState
        {
            None = 0,
            Random = 1,
            AnimationList = 1 << 1,
            Speed = 1 << 2,
            FlyState = 1 << 3,
            All = Random | AnimationList | Speed | FlyState,
        }
        #endregion
    }
}