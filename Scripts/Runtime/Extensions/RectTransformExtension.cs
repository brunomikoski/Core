using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BrunoMikoski
{
    public static class RectTransformExtension
    {
        public static void MarkLayoutForBuild(this RectTransform rectTransform)
        {
            MarkLayoutForBuild_Async(rectTransform, rectTransform.gameObject.GetCancellationTokenOnDestroy()).Forget();
        }

        private static async UniTaskVoid MarkLayoutForBuild_Async(RectTransform rectTransform, CancellationToken cancellationToken = default)
        {
            await UniTask.Yield(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            
            if (rectTransform.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
            }
        }
    }
}
