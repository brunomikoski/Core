using System.Threading;
#if UNITASK_AVAILABLE
using Cysharp.Threading.Tasks;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace BrunoMikoski.Core.Utils
{
    public static class RectTransformExtension
    {
        public static void MarkLayoutForBuild(this RectTransform rectTransform)
        {
#if UNITASK_AVAILABLE
            MarkLayoutForBuild_Async(rectTransform, rectTransform.gameObject.GetCancellationTokenOnDestroy()).Forget();
#endif
        }

#if UNITASK_AVAILABLE
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
#endif
    }
}
