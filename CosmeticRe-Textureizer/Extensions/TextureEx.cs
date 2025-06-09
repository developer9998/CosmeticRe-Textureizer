using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cosmetic_ReTextureizer.Extensions
{
    public static class TextureEx
    {
        public static async Task<Texture2D> ToTexture(this Material material, int width, int height, FilterMode filterMode = FilterMode.Point)
        {
            if (!SystemInfo.supportsAsyncGPUReadback)
                throw new NotImplementedException("GetTexture does not support lack of AsyncGPUReadback");

            if (material is null)
                throw new ArgumentNullException(nameof(material));

            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            renderTexture.Create();

            Graphics.Blit(null, renderTexture, material, 0); // That last zero for the depth pass is essential for this

            Texture2D texture;

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                TaskCompletionSource<AsyncGPUReadbackRequest> taskCompletionSource = new();
                AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, taskCompletionSource.SetResult);
                AsyncGPUReadbackRequest request = await taskCompletionSource.Task;

                RenderTexture.ReleaseTemporary(renderTexture);

                NativeArray<byte> data = request.GetData<byte>();
                texture = new(width, height, TextureFormat.RGB24, false)
                {
                    filterMode = filterMode
                };
                texture.LoadRawTextureData(data);
                texture.Apply();

                return texture;
            }

            RenderTexture active = RenderTexture.active;
            RenderTexture.active = renderTexture;

            texture = new(width, height, TextureFormat.RGB24, false)
            {
                filterMode = filterMode
            };
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();

            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(renderTexture);

            return null;
        }
    }
}
