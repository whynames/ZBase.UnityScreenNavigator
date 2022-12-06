using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZBase.UnityScreenNavigator.Foundation;
using ZBase.UnityScreenNavigator.Foundation.Animation;

namespace ZBase.UnityScreenNavigator.Core.Shared
{
    public interface ITransitionAnimation : IAnimation
    {
        void SetPartner(RectTransform partnerRectTransform);
        
        void Setup(RectTransform rectTransform);
    }

    internal static class TransitionAnimationExtensions
    {
        public static IEnumerator CreatePlayRoutine(this ITransitionAnimation self, IProgress<float> progress = null)
        {
            var player = Pool<AnimationPlayer>.Shared.Rent();
            player.Initialize(self);

            UpdateDispatcher.Instance.Register(player);
            progress?.Report(0.0f);
            player.Play();

            while (player.IsFinished == false)
            {
                yield return null;
                progress?.Report(player.Time / self.Duration);
            }

            UpdateDispatcher.Instance.Unregister(player);
            Pool<AnimationPlayer>.Shared.Return(player);
        }

        public static async UniTask PlayAsync(this ITransitionAnimation self, IProgress<float> progress = null)
        {
            var player = Pool<AnimationPlayer>.Shared.Rent();
            player.Initialize(self);

            progress?.Report(0.0f);
            player.Play();

            while (player.IsFinished == false)
            {
                await UniTask.NextFrame();
                player.Update(Time.unscaledDeltaTime);
                progress?.Report(player.Time / self.Duration);
            }

            Pool<AnimationPlayer>.Shared.Return(player);
        }
    }
}