using System;
using WzComparerR2.Animation;
using WzComparerR2.MapRender.Patches2;

namespace WzComparerR2.MapRender
{
    public class MobBehaviorBinding
    {
        public MobBehaviorBinding(LifeItem life, Action<int, string> playSoundEff, Action<BehaviorController> trySummonMob, Action<LifeItem, int, int> requestMoveLayer, Action<SceneItem> requestRemoveFromLayer)
        {
            this.life = life;
            this.animator = life.View.Animator as StateMachineAnimator;
            this.controller = life.Controller;
            this.playSoundEff = playSoundEff;
            this.trySummonMob = trySummonMob;
            this.requestMoveLayer = requestMoveLayer;
            this.requestRemoveFromLayer = requestRemoveFromLayer;
        }

        private readonly LifeItem life;
        private readonly StateMachineAnimator animator;
        private readonly BehaviorController controller;
        private readonly Action<int, string> playSoundEff;
        private readonly Action<BehaviorController> trySummonMob;
        private readonly Action<LifeItem, int, int> requestMoveLayer;
        private readonly Action<SceneItem> requestRemoveFromLayer;

        public void Bind()
        {
            if (this.animator == null || this.controller == null)
            {
                return;
            }

            this.controller.SetAnimationList(this.animator.Data.States);

            this.controller.MobHit += (o, e) =>
            {
                string aniName = Prefer("hit1", "hit");
                if (aniName == null)
                {
                    this.controller.RecoverHit();
                    return;
                }
                SetAni(aniName);
                this.playSoundEff(this.controller.ID, "Damage");
            };

            this.controller.StateChanged += (o, e) =>
            {
                var bState = e.BState;
                var hState = e.HState;
                var vState = e.VState;
                var pState = e.PState;
                string aniName;
                string defaultMoveAni = pState == BehaviorController.ProvokeState.Chase ? "chase" : "move";

                switch (bState)
                {
                    case BehaviorController.BaseState.Idle: // goto check VState if Idle else return
                        break;

                    case BehaviorController.BaseState.Regen:
                        aniName = Prefer("regen");
                        if (aniName == null)
                        {
                            this.controller.EndRegen();
                            return;
                        }
                        if (SetIfDifferent(aniName) && this.controller.PlayRegenSound)
                        {
                            this.playSoundEff(this.controller.ID, "Regen");
                        }
                        return;

                    case BehaviorController.BaseState.Hit:
                        aniName = Prefer("hit1", "hit");
                        if (aniName == null)
                        {
                            this.controller.RecoverHit();
                            return;
                        }
                        SetIfDifferent(aniName);
                        return;

                    case BehaviorController.BaseState.Died:
                        aniName = Prefer("die1", "die");
                        if (aniName == null)
                        {
                            this.trySummonMob(this.controller);
                            if (this.controller.NoRegen)
                            {
                                this.requestRemoveFromLayer(this.controller.Owner);
                                break;
                            }
                            this.controller.RecoverDied();
                            return;
                        }
                        if (SetIfDifferent(aniName) && this.controller.PlayDieSound)
                        {
                            this.playSoundEff(this.controller.ID, "Die");
                        }
                        return;

                    case BehaviorController.BaseState.Attack:
                        aniName = Prefer(this.controller.SelectedAttack);
                        if (aniName == null)
                        {
                            this.controller.EndAttack();
                            return;
                        }
                        if (SetIfDifferent(aniName))
                        {
                            this.playSoundEff(this.controller.ID, this.controller.SelectedAttack.Replace("attack", "Attack").Replace("skill", "Skill"));
                        }
                        SetEffectIfDifferent(aniName + "_effect");
                        return;

                    default:
                        goto case BehaviorController.BaseState.Idle;
                }

                switch (vState)
                {
                    case BehaviorController.VerticalState.Fly:
                        SetIfDifferent(Prefer("fly"));
                        break;

                    case BehaviorController.VerticalState.Jump:
                    case BehaviorController.VerticalState.Fall:
                        SetIfDifferent(Prefer("jump", "fly", "stand", "move"));
                        break;

                    case BehaviorController.VerticalState.Stop:
                        if (hState == BehaviorController.HorizontalState.MoveL || hState == BehaviorController.HorizontalState.MoveR)
                        {
                            SetIfDifferent(Prefer(defaultMoveAni, "fly"));
                            if (e.StateType == BehaviorController.StateType.Horizontal && (BehaviorController.HorizontalState)e.PrevState == BehaviorController.HorizontalState.Stop)
                            {
                                this.playSoundEff(this.controller.ID, "Move");
                            }
                        }
                        else
                        {
                            SetIfDifferent(Prefer("stand", "fly"));
                        }
                        break;

                    default:
                        goto case BehaviorController.VerticalState.Stop;
                }
            };

            this.controller.LayerChanged += (o, e) =>
            {
                var prevLayer = e.PrevLayer;
                var newLayer = e.NewLayer;
                this.requestMoveLayer(this.controller.Owner, prevLayer, newLayer);
            };

            this.animator.AnimationEnd += (o, e) =>
            {
                switch (e.CurrentState)
                {
                    case "regen":
                        this.controller.EndRegen();
                        break;

                    case "stand":
                    case "fly":
                    case "jump":
                    case "chase":
                        e.NextState = e.CurrentState;
                        break;

                    case "move":
                        e.NextState = e.CurrentState;
                        this.playSoundEff(this.controller.ID, "Move");
                        break;

                    case "hit1":
                    case "hit":
                        this.controller.RecoverHit();
                        break;

                    case "die1":
                    case "die":
                        this.trySummonMob(this.controller);
                        if (this.controller.NoRegen)
                        {
                            this.requestRemoveFromLayer(this.controller.Owner);
                            break;
                        }
                        this.controller.RecoverDied();
                        break;

                    case string s when s.StartsWith("attack") || s.StartsWith("skill"):
                        this.controller.EndAttack();
                        break;

                    default:
                        goto case "regen";
                }
            };

            this.animator.EffectAnimationEnd += (o, e) =>
            {
                switch (e.CurrentState)
                {
                    case string s when s.Contains("effect"):
                        e.NextState = "invisible";
                        break;

                    default:
                        break;
                }
            };
        }

        private string Prefer(params string[] names)
        {
            foreach (var n in names)
            {
                if (this.animator.Data.States.Contains(n))
                {
                    return n;
                }
            }
            return null;
        }

        private bool SetAni(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }
            this.animator.SetAnimation(name);
            return true;
        }

        private bool SetIfDifferent(string name)
        {
            if (string.IsNullOrEmpty(name) || this.animator.GetCurrent() == name)
            {
                return false;
            }
            this.animator.SetAnimation(name);
            return true;
        }

        private bool SetEffectIfDifferent(string name)
        {
            if (string.IsNullOrEmpty(name) || this.animator.GetCurrentEffect() == name)
            {
                return false;
            }
            this.animator.SetEffectAnimation(name);
            return true;
        }
    }
}
