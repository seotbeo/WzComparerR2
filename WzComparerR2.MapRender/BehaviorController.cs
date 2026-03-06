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
            this.bState = playRegenMotion ? BaseState.Regen : BaseState.Idle;
            this.hState = HorizontalState.Stop;
            this.vState = VerticalState.Stop;
            this.pState = ProvokeState.None;

            this.Owner = life;
            this.ID = life.ID;

            this.FHManager = fhManager;

            this.basePos = new Vector2(life.X, life.Cy);
            this.relPos = Vector2.Zero;
            this.baseFoothold = life.Fh;
            this.baseFootholdGroup = FHManager.GetGroupIndexByFootholdIndex(this.baseFoothold);
            this.availableArea = this.FHManager.Area;

            InitCurFoothold(this.basePos);

            this.minMovePosX = this.Summoned ? FHManager.GetGroupByIndex(this.curFootholdGroup)?.GroupArea.Left ?? this.availableArea.Left : life.Rx0;
            this.maxMovePosX = this.Summoned ? FHManager.GetGroupByIndex(this.curFootholdGroup)?.GroupArea.Right ?? this.availableArea.Right : life.Rx1;
            if (this.basePos.X < this.minMovePosX)
            {
                this.minMovePosX = (int)this.basePos.X;
            }
            else if (this.basePos.X > this.maxMovePosX)
            {
                this.maxMovePosX = (int)this.basePos.X;
            }

            this.curLayer = FHManager.GetLayerByFootholdIndex(this.curFoothold);
            this.curLayerFoothold = this.curFoothold;
        }

        #region Consts
        private const int Walk_Force = 1400;
        private const int Walk_Drag = 800;
        private const int SpeedBase = 125;
        private const int Fly_SpeedBase = 200;
        private const float Fly_WavePeriod = 720f;
        private const int Fly_WaveHeightBase = 30;
        private const int Fly_WaveHeightRange = 15;
        private const float Fly_FindTargetProb = 0.0004f;
        private const int Fly_FindTargetX = 400;
        private const int Fly_FindTargetY = 500;
        private const int Max_FallSpeed = 670;
        private const int JumpSpeed = 555;
        private const int GravityAcc = 2000;
        private const float RandomJumpProb = 0.003f;
        private const int MaxTriggerDistance = 30;
        private const int MinTriggerDistance = 10;
        private const int DamageBase = 70;
        private const int DamageRange = 50;
        private const float RandomRestTimeBase = 2.8f;
        private const float RandomRestTimeRange = 2f;
        private const int PosGuardMargin = 500;
        #endregion

        #region Inner States
        private int inited = 0;
        private BaseState bState;
        private HorizontalState hState;
        private VerticalState vState;
        private ProvokeState pState;

        private float vSpeed = 0;
        private int hp = 100;

        private TimeSpan restTime;

        private int curLayer = -1;
        private int curLayerFoothold = -1;
        private int curFoothold = -1;
        private int curFootholdGroup = -1;
        private Vector2 relPos;

        private int attackIdx = -1;
        private int attackType = -1;

        private int fly_WaveHeight;
        private int fly_TargetFoothold = -1;
        private int fly_TargetDir;
        private float fly_PrevPhase = 1;
        private float fly_ToTargetSpeedX;
        private float fly_ToTargetSpeedY;
        private Vector2 fly_TargetPos;
        private TimeSpan flyingPhaseTime;

        private bool HasMoveTarget; // 이동 목표까지 상태 결정 차단
        #endregion

        private ReadOnlyCollection<string> aniList;
        private ReadOnlyCollection<string> attackList;
        private ReadOnlyCollection<string> skillList;

        private readonly TimeSpan MaxFrameRate = TimeSpan.FromMilliseconds(50);
        private readonly FootholdManager FHManager;
        private IRandom Random;

        private readonly int baseFoothold;
        private readonly int baseFootholdGroup;

        private readonly Vector2 basePos;
        private readonly Rectangle availableArea;

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

        private bool noRegen;

        private int finalSpeed => this.flying ? Fly_SpeedBase + this.flySpeed : (SpeedBase + (this.chasing ? this.chaseSpeed : this.speed));
        private bool grounded => this.vState == VerticalState.Stop;
        private bool flying => this.vState == VerticalState.Fly;
        private bool flyingToTarget => this.fly_TargetFoothold != -1;
        private bool floating => this.vState == VerticalState.Jump || this.vState == VerticalState.Fall;
        private bool jumping => this.vState == VerticalState.Jump;
        private bool falling => this.vState == VerticalState.Fall;
        private bool chasing => this.pState == ProvokeState.Chase;

        public LifeItem Owner { get; }
        public int ID { get; }
        public bool NoRegen => this.Summoned;
        public bool Summoned { get; }
        public bool PlayRegenMotion { get; }
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
        public bool Inited => this.inited == 0b111;
        public int CurFoothold => this.curFoothold;
        public int CurLayerFoothold => this.curLayerFoothold;
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
            this.inited |= 0b001;
        }

        public void SetAnimationList(ReadOnlyCollection<string> ani)
        {
            this.aniList = ani;
            this.hasMoveMotion = aniList.Contains("move");
            this.hasJumpMotion = aniList.Contains("jump");
            this.hasFlyMotion = aniList.Contains("fly");
            if (this.CanFly)
            {
                SetVerticalState(VerticalState.Fly);
                if (this.Summoned)
                {
                    this.minMovePosX = this.availableArea.Left;
                    this.maxMovePosX = this.availableArea.Right;
                }
            }
            this.hasChaseMotion = aniList.Contains("chase");
            this.attackList = aniList.Where(a => a.StartsWith("attack")).ToList().AsReadOnly();
            this.skillList = aniList.Where(a => a.StartsWith("skill")).ToList().AsReadOnly();
            this.hasAttackMotion = attackList.Count > 0;
            this.hasSkillMotion = skillList.Count > 0;
            this.inited |= 0b010;
        }

        public void SetSpeed(int speed, int flySpeed, int chaseSpeed)
        {
            this.speed = speed;
            this.flySpeed = flySpeed;
            this.chaseSpeed = chaseSpeed;
            this.inited |= 0b100;
        }

        public void Reset()
        {
            SetBaseState(BaseState.Regen);
            SetHorizontalState(HorizontalState.Stop);
            SetVerticalState(this.flying ? VerticalState.Fly : VerticalState.Stop);
            SetProvokeState(ProvokeState.None);
            this.HasMoveTarget = false;
            this.fly_TargetFoothold = -1;
            this.relPos = Vector2.Zero;
            this.flyingPhaseTime = TimeSpan.Zero;
            InitCurFoothold(this.basePos);
            this.hp = 100;
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
            if (this.bState == BaseState.Regen || this.bState == BaseState.Died) return;
            if (elapsedTime > MaxFrameRate) elapsedTime = MaxFrameRate;

            if (this.MovementEnabled && !this.Fixed)
            {
                var prevPos = CurPos;
                Move(elapsedTime, prevPos);
            }

            if (this.ForceMoveStop || this.HasMoveTarget) return;

            this.flyingPhaseTime += elapsedTime;
            this.restTime -= elapsedTime;
            if (this.restTime <= TimeSpan.Zero)
            {
                DecideState(CurPos);
                this.restTime = TimeSpan.FromSeconds(this.Random.NextVar(RandomRestTimeBase, RandomRestTimeRange, true));
            }
        }

        private void DecideState(Vector2 pos)
        {
            var coef = this.Random.Next(4);
            if (this.grounded || this.flying)
            {
                if (this.chasing || this.flying)
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
            MoveX(elapsedTime, prevPos);
            MoveY(elapsedTime, prevPos);
            FlyToTarget(elapsedTime);
            DecideFlyToTarget(CurPos);
            DecideJump(CurPos);
            RandomJump();
            ExecuteOthers();
            PosGuard();
        }

        private void MoveX(TimeSpan elapsedTime, Vector2 prevPos)
        {
            if (this.hState != HorizontalState.Stop && !this.ForceMoveStop && !this.flyingToTarget)
            {
                var dir = this.hState == HorizontalState.MoveL ? -1 : 1;
                var newX = this.relPos.X + dir * Math.Max(0, this.finalSpeed) * (float)elapsedTime.TotalSeconds;

                var isOutOfRange = IsEndOfAvailableRange(basePos.X + newX);
                if (this.floating) // 점프 중 같은 그룹 내 수직 발판과 충돌 확인
                {
                    if (HCollisionTest(CurPos, new Vector2(basePos.X + newX, CurPos.Y)) || isOutOfRange)
                    {
                        SetHorizontalState(HorizontalState.Stop);
                        return;
                    }
                    else
                    {
                        this.relPos.X = newX;
                        return;
                    }
                }

                if (isOutOfRange) // 가능 범위 밖이면 무조건 flip
                {
                    DoFlipX();
                    return;
                }

                var nextFH = GetNextFootholdIndex(this.curFoothold, dir, basePos.X + newX);
                var IsEndOfCurFoothold = nextFH == -1;
                if (IsEndOfCurFoothold && FHManager.GetFootholdByID(this.curFoothold, out var curfh)) // 이어진 발판 끝인 경우
                {
                    var canJumpOrFall = this.CanJump;
                    if (this.flying || !canJumpOrFall)
                    {
                        DoFlipX();
                        return;
                    }
                    else
                    {
                        if (IsLastFoothold(curfh, dir)) // 그룹 내 마지막 발판인 경우 점프 가능 else 낙하 가능
                        {
                            canJumpOrFall = canJumpOrFall && HasPossibleFoothold(new Vector2(basePos.X + newX, this.CurPos.Y), dir, 1000, -75, 1500, -JumpSpeed); // 점프 가능 발판 있는지 탐색
                            if (canJumpOrFall)
                            {
                                DoJump();
                                this.relPos.X = newX;
                                return;
                            }
                            else
                            {
                                DoFlipX();
                                return;
                            }
                        }
                        else
                        {
                            if (canJumpOrFall) // 낙하는 아래쪽으로만
                            {
                                if (FHManager.GetFootholdByID(dir < 0 ? curfh.Prev : curfh.Next, out var nextfh))
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
                                return;
                            }
                        }
                    }
                }
                else // 이어진 발판 내 이동
                {
                    if (nextFH != this.curFoothold)
                    {
                        SetCurFoothold(nextFH, FHManager.GetGroupIndexByFootholdIndex(nextFH));
                    }
                    this.relPos.X = newX;
                    return;
                }
            }
        }

        private void MoveY(TimeSpan elapsedTime, Vector2 prevPos)
        {
            if (this.grounded && !this.ForceMoveStop)
            {
                var newY = GetRelYOnFoothold(CurPos.X, CurPos.Y);
                this.relPos.Y += newY;
            }
            else if (this.flying && !this.ForceMoveStop && !this.flyingToTarget)
            {
                var newY = GetRelYOnFoothold(CurPos.X, CurPos.Y);
                newY += GetRelYOnFlyingWave((float)(this.flyingPhaseTime.TotalMilliseconds));
                this.relPos.Y += newY;
            }
            else if (this.jumping)
            {
                this.vSpeed += GravityAcc * (float)elapsedTime.TotalSeconds;

                var newY = this.vSpeed * (float)elapsedTime.TotalSeconds;
                this.relPos.Y += newY;
                if (this.vSpeed >= 0)
                {
                    SetVerticalState(VerticalState.Fall);
                }
            }
            else if (this.falling)
            {
                this.vSpeed += GravityAcc * (float)elapsedTime.TotalSeconds;
                this.vSpeed = Math.Min(this.vSpeed, Max_FallSpeed);

                var newY = this.vSpeed * (float)elapsedTime.TotalSeconds;

                VCollisionTest(prevPos, new Vector2(CurPos.X, CurPos.Y + newY));

                this.relPos.Y += newY;
                var collisionOn = GetRelYOnFoothold(CurPos.X, basePos.Y);
                if (this.relPos.Y > collisionOn)
                {
                    this.relPos.Y = collisionOn;
                    this.vSpeed = 0;
                    this.HasMoveTarget = false;
                    SetVerticalState(VerticalState.Stop);
                }
            }
        }

        private void FlyToTarget(TimeSpan elapsedTime)
        {
            if (this.flyingToTarget && !this.ForceMoveStop)
            {
                var newX = this.relPos.X + this.fly_ToTargetSpeedX * (float)elapsedTime.TotalSeconds;
                var newY = this.relPos.Y + this.fly_ToTargetSpeedY * (float)elapsedTime.TotalSeconds;

                if (this.fly_TargetDir * (this.fly_TargetPos.X - (this.basePos.X + newX)) <= 0) // end flying
                {
                    this.relPos = this.fly_TargetPos - this.basePos;
                    EndFlyTarget();
                }
                else
                {
                    this.relPos.X = newX;
                    this.relPos.Y = newY;
                }
            }
        }

        private void DoFlipX()
        {
            if (this.hState == HorizontalState.MoveL) this.hState = HorizontalState.MoveR;
            else this.hState = HorizontalState.MoveL;
            this.FlipX = !this.FlipX;
            this.restTime += TimeSpan.FromSeconds(RandomRestTimeBase - RandomRestTimeRange);
        }

        private int GetNextFootholdIndex(int curFootholdIndex, int dir, float x)
        {
            var index = curFootholdIndex;
            FootholdItem fh;
            if (FHManager.GetFootholdByID(index, out fh))
            {
                if (dir < 0)
                {
                    index = fh.Prev;
                    while (true)
                    {
                        if (x >= fh.X1)
                        {
                            return fh.ID;
                        }
                        else
                        {
                            if (index != 0 && (FHManager.GetFootholdByID(index, out fh)))
                            {
                                if (fh.IsWall)
                                {
                                    return -1;
                                }
                                else
                                {
                                    index = fh.Prev;
                                    continue;
                                }
                            }
                            else return -1;
                        }
                    }
                }
                else if (dir > 0)
                {
                    index = fh.Next;
                    while (true)
                    {
                        if (x <= fh.X2)
                        {
                            return fh.ID;
                        }
                        else
                        {
                            if (index != 0 && (FHManager.GetFootholdByID(index, out fh)))
                            {
                                if (fh.IsWall)
                                {
                                    return -1;
                                }
                                else
                                {
                                    index = fh.Next;
                                    continue;
                                }
                            }
                            else return -1;
                        }
                    }
                }
            }

            return index;
        }

        private bool IsEndOfAvailableRange(float x)
        {
            if (x < this.minMovePosX || x > this.maxMovePosX)
                return true;

            return false;
        }

        private bool IsLastFoothold(int index, int dir)
        {
            if (FHManager.GetFootholdByID(index, out var fh))
            {
                return IsLastFoothold(fh, dir);
            }

            return false;
        }

        private bool IsLastFoothold(FootholdItem fh, int dir)
        {
            if (dir < 0)
            {
                if (fh.Prev == 0)
                {
                    return true;
                }
            }
            else if (dir > 0)
            {
                if (fh.Next == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateCurFootholdInGroup(float x)
        {
            if (FHManager.GetFootholdByID(curFoothold, out var fh))
            {
                if (x < fh.X1 && fh.Prev != 0 && !fh.IsWall)
                {
                    curFoothold = fh.Prev;
                }
                else if (x > fh.X2 && fh.Next != 0 && !fh.IsWall)
                {
                    curFoothold = fh.Next;
                }
            }
        }

        private int VRayCastingTest(Vector2 pos)
        {
            FootholdItem selectedBelow = null;
            var belowY = int.MaxValue;
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
                }
            }
            return selectedBelow?.GroupIndex ?? -1;
        }

        private bool HCollisionTest(Vector2 prevPos, Vector2 nextPos)
        {
            int gi = -1;
            if (this.grounded) gi = this.curFootholdGroup;
            else  gi = VRayCastingTest(prevPos);

            if (gi != -1)
            {
                foreach (var group in FHManager.AllFootholdGroups.Where(g => g.Index == gi))
                {
                    foreach (var fh in group.Footholds)
                    {
                        if (fh.IsWall)
                        {
                            if (FootholdManager.Intersects(fh, prevPos, nextPos))
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool VCollisionTest(Vector2 prevPos, Vector2 nextPos)
        {
            foreach (var group in FHManager.AllFootholdGroups.Where(g => FootholdManager.GetCandidateGroups(g, prevPos, nextPos)))
            {
                foreach (var fh in group.Footholds)
                {
                    if (!fh.IsWall && FootholdManager.Intersects(fh, prevPos, nextPos))
                    {
                        HandleVCollision(fh);
                        return true;
                    }
                }
            }
            return false;
        }

        private void HandleVCollision(FootholdItem fh)
        {
            SetCurFoothold(fh.ID, fh.GroupIndex);
        }

        private int GetRelYOnFoothold(float x, float y)
        {
            if (FHManager.GetFootholdByID(curFoothold, out var fh) && !fh.IsWall)
            {
                return (int)(FHManager.GetYOnFoothold(fh, x) - y);
            }
            return int.MaxValue;
        }

        private int GetRelYOnFlyingWave(float phase)
        {
            phase %= Fly_WavePeriod;
            phase = MathHelper.Clamp(phase / Fly_WavePeriod, 0f, 1f);

            float arc = -4f * phase * (1f - phase) * GetFlyWaveHeight(phase);

            return (int)arc;
        }

        private int GetFlyWaveHeight(float phase)
        {
            if (phase < fly_PrevPhase) // 웨이브 높이 재설정
            {
                fly_WaveHeight = this.Random.NextVar(Fly_WaveHeightBase, Fly_WaveHeightRange, true);
            }
            fly_PrevPhase = phase;
            return fly_WaveHeight;
        }

        private void DecideJump(Vector2 pos)
        {
            if (!this.grounded || !this.CanJump || this.HasMoveTarget || this.hState == HorizontalState.Stop) return;

            bool canJump = false;
            int x = (int)pos.X;
            int dir = this.hState == HorizontalState.MoveL ? -1 : 1;
            if ((x >= this.minMovePosX || dir > 0) &&
                (x <= this.maxMovePosX || dir < 0) &&
                FHManager.GetFootholdByID(curFoothold, out var fh))
            {
                FootholdItem nextfh = fh;
                FootholdItem endfh = fh;
                Vector2 fallPos = pos;
                var next = dir < 0 ? fh.Prev : fh.Next;

                while (true) // 발판 끝부터 거리 확인
                {
                    if (next == 0)
                    {
                        endfh = nextfh;
                        var d = Math.Abs((dir < 0 ? nextfh.X1 : nextfh.X2) - x);
                        if (!(d <= MaxTriggerDistance && d >= MinTriggerDistance))
                        {
                            return;
                        }
                        else break;
                    }
                    else if (FHManager.GetFootholdByID(next, out nextfh))
                    {
                        if (nextfh.IsWall)
                        {
                            var d = Math.Abs((dir < 0 ? nextfh.X1 : nextfh.X2) - x);
                            if (d <= MaxTriggerDistance && d >= MinTriggerDistance)
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
                canJump = HasPossibleFoothold(new Vector2(x, this.CurPos.Y), dir, Math.Abs(limitX1), -75, 0, -JumpSpeed, sameGroup: true); // 점프 가능 발판 있는지 탐색
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
            var dt = 1 / 60f; // 60프레임으로 이동경로 시뮬레이션
            var timestamps = Math.Max(1, Math.Max((int)Math.Ceiling(limitX / Math.Max(1f, hSpeed * dt)), (int)Math.Ceiling((maxLimitY - minLimitY) / Math.Max(1f, Max_FallSpeed * dt))));
            var hPos = pos.X;
            var vPos = pos.Y;
            var prevPos = pos;
            var nextPos = pos;

            for (int i = 0; i < timestamps; i++)
            {
                prevPos = nextPos;
                hPos += dir * hSpeed * (float)dt;
                hPos = Math.Min(Math.Max(hPos, this.minMovePosX), this.maxMovePosX);
                vSpeed += GravityAcc * (float)dt;
                vSpeed = Math.Min(vSpeed, Max_FallSpeed);
                vPos += vSpeed * (float)dt;
                nextPos = new Vector2(hPos, vPos);

                if (vSpeed < 0) continue;
                if ((nextPos.X <= minX) || (nextPos.X >= maxX)) break;
                if ((nextPos.Y <= minY) || (nextPos.Y >= maxY)) break;

                foreach (var group in FHManager.AllFootholdGroups.Where(g => (sameGroup ? g.Index == this.curFootholdGroup : true) && FootholdManager.GetCandidateGroups(g, prevPos, nextPos)))
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
            this.curFoothold = -1;
            SetVerticalState(VerticalState.Jump);
        }

        private void DoFall()
        {
            this.curFoothold = -1;
            SetVerticalState(VerticalState.Fall);
        }

        private void DecideFlyToTarget(Vector2 pos)
        {
            if (this.flying && !this.HasMoveTarget)
            {
                if (this.Random.NextPercent(Fly_FindTargetProb))
                {
                    SetFlyTarget(pos);
                    return;
                }
            }
        }

        private void SetFlyTarget(Vector2 pos)
        {
            var margin = new Vector2(Fly_FindTargetX, Fly_FindTargetY);
            var candidateFHs = FHManager.AllFootholdGroups.Where(g => g.Index != this.curFootholdGroup && FootholdManager.GetCandidateGroups(g, pos - margin, pos + margin, margin: 0)).SelectMany(g => g.Footholds)
                .Where(f => !f.IsWall && Math.Min(f.X1, f.X2) >= this.minMovePosX && Math.Max(f.X1, f.X2) <= this.maxMovePosX && FootholdManager.GetCandidateFootholds(f, pos - margin, pos + margin, margin: 0)).ToList();
            var candidateCount = candidateFHs.Count;
            if (candidateCount > 0)
            {
                var selected = candidateFHs[this.Random.Next(candidateCount)];

                if (selected != null)
                {
                    var x = (selected.X1 + selected.X2) / 2;
                    var y = (selected.Y1 + selected.Y2) / 2;
                    var dx = x - pos.X;
                    var dy = y - pos.Y;
                    var dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    this.fly_TargetFoothold = selected.ID;
                    this.fly_TargetPos = new Vector2(x, y);
                    this.fly_TargetDir = dx < 0 ? -1 : 1;
                    this.fly_ToTargetSpeedX = this.finalSpeed / dist * dx;
                    this.fly_ToTargetSpeedY = this.finalSpeed / dist * dy;
                    if (fly_TargetDir < 0 && this.HState == HorizontalState.MoveR ||
                        fly_TargetDir > 0 && this.HState == HorizontalState.MoveL)
                    {
                        DoFlipX();
                    }
                    SetCurFoothold(-1, -1);
                    this.HasMoveTarget = true;
                }
            }
        }

        private void EndFlyTarget()
        {
            SetCurFoothold(this.fly_TargetFoothold, FHManager.GetGroupIndexByFootholdIndex(this.fly_TargetFoothold));
            this.fly_TargetFoothold = -1;
            this.HasMoveTarget = false;
            this.flyingPhaseTime = TimeSpan.Zero;
        }

        private void ExecuteOthers()
        {

        }

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

        private void SetCurFoothold(int newID, int newGroup)
        {
            if (this.curFoothold == newID) return;
            var prevGroup = this.curFootholdGroup;
            this.curFoothold = newID;
            this.curFootholdGroup = newGroup;
            SetLayer(this.curFoothold);
        }

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
                    finalSelected = (belowY + upperY) / 2 <= pos.Y ? selectedBelow : selectedUpper;
                else finalSelected = selectedBelow ?? selectedUpper;

                if (finalSelected != null)
                {
                    SetCurFoothold(finalSelected.ID, FHManager.GetGroupIndexByFootholdIndex(finalSelected.ID));
                    SetVerticalState(this.flying ? VerticalState.Fly : this.MovementEnabled ? VerticalState.Fall : VerticalState.Stop);
                    return;
                }
            }

            // 초기 발판 찾기 실패
            this.curFoothold = -1;
            this.curFootholdGroup = -1;
            this.Fixed = true;
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

        private void SetLayer(int nextfh, bool invoke = true)
        {
            var nextLayer = FHManager.GetLayerByFootholdIndex(nextfh);
            if (this.curLayer == nextLayer) return;
            this.curLayer = nextLayer;
            var prevfh = this.curLayerFoothold;
            this.curLayerFoothold = nextfh;
            if (invoke) OnLayerChanged(new LayerChangedEventArgs(prevfh, nextfh));
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

        private enum CommandState
        {
            None,
            Queued,
            Execute,
        }
        #endregion
    }
}