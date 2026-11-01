using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Analytics
{
    public class HeatMapRecorder : GameSystemComponent
    {
        [SerializeField] private bool collectData = false;

        private static Gradient heatMapGradient;

        private int[] heatMap;
        int scaledWidth;
        int scaledHeight;

        private void Start()
        {
            if(collectData == false)
            {
                Debug.Log("HeatMapRecorder disabled");
                gameObject.SetActiveOptimized(false);
                return;
            }

            SetupHeatmapVariables();
            SetupGradient();
        }

        private void FixedUpdate()
        {
            Vector2Int normalizedCursorPosition = new Vector2Int(
                (int)(Input.mousePosition.x / Screen.width * scaledWidth),
                (int)(Input.mousePosition.y / Screen.height * scaledHeight));

            IncreaseHeatMapPoint(normalizedCursorPosition.x, normalizedCursorPosition.y);
        }

        private void OnDestroy()
        {
            if (collectData == false)
                return;

            SaveHeatMapFile(System.DateTime.Now.ToString("yyyyMMddHHmmssffff"));
            Debug.LogError("Destroy");
        }

        private void SetupHeatmapVariables()
        {
            scaledWidth = Screen.width / 10;
            scaledHeight = Screen.height / 10;

            heatMap = new int[scaledWidth * scaledHeight];
        }

        private void SetupGradient()
        {
            heatMapGradient = new Gradient();

            GradientColorKey[] gradientColorKeys = {
                    new GradientColorKey(Color.grey, 0),
                    new GradientColorKey(Color.yellow, 0.25f),
                    new GradientColorKey(Color.red, 1)
                };

            GradientAlphaKey[] gradientAlphaKeys = {
                    new GradientAlphaKey(0, 0),
                    new GradientAlphaKey(0.24f, 0.1f),
                    new GradientAlphaKey(0.62f, 0.3f),
                    new GradientAlphaKey(0.81f, 0.5f),
                    new GradientAlphaKey(0.86f, 0.8f)
                };

            heatMapGradient.SetKeys(gradientColorKeys, gradientAlphaKeys);
        }

        private void IncreaseHeatMapPoint(int x, int y)
        {
            if (x < 0 || x > scaledWidth)
                return;

            if (y < 0 || y > scaledHeight)
                return;

            heatMap[y * scaledWidth + x]++;
        }

        private void SaveHeatMapFile(string name)
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            string dirPath = Application.persistentDataPath + @"/Analytics/";
            string fileName = $"heatmap_{name}.png";
            string fullPath = dirPath + fileName;

            Texture2D heatMapTexture = GetHeatMapTexture();

            byte[] bytes = heatMapTexture.EncodeToPNG();

            if (Directory.Exists(dirPath) == false)
                Directory.CreateDirectory(dirPath);

            try
            {
                File.WriteAllBytes(fullPath, bytes);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            stopwatch.Stop();
            Debug.Log($"Heatmap exported in {stopwatch.ElapsedMilliseconds}ms\n{fullPath}");
            System.Diagnostics.Process.Start(dirPath);
        }

        private Texture2D GetHeatMapTexture()
        {
            Texture2D texture = new Texture2D(Screen.width, Screen.height);
            float maxValue = heatMap.Max();
            int[,] heatMap2D = GetHeatMap2D();

            maxValue *= 0.9f;
            maxValue++;

            for (int w = 0; w < Screen.width; w++)
            {
                for (int h = 0; h < Screen.height; h++)
                {
                    texture.SetPixel(w, h, heatMapGradient.Evaluate(Mathf.Clamp01(heatMap2D[w / 10, h / 10] / maxValue)));
                }
            }

            texture.Apply();

            return texture;
        }

        private int[,] GetHeatMap2D()
        {
            int[,] tempHeatMap = new int[scaledWidth, scaledHeight];

            for (int y = 0; y < scaledHeight; y++)
            {
                for (int x = 0; x < scaledWidth; x++)
                {
                    tempHeatMap[x, y] = heatMap[y * scaledWidth + x];
                }
            }

            return tempHeatMap;
        }
    }
}
