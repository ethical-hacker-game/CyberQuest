using RuntimeInspectorNamespace;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

[Serializable]
public class AppScenario : IValidateContent
{
    public string appID;
    public string scenarioID;
    public string header;
    public string extension;
    public Vector2Int size;
    [InfoString("Left, Bottom, Right, Top")] public Vector4 margin;
    public List<Stage> stages = new List<Stage>();

    private AppCore appCore;
    private string contentPath;
    public AppCore AppCore { get => appCore; }
    public string ScenarioPath
    {
        get
        {
            if (string.IsNullOrEmpty(contentPath) == false)
                return contentPath;

            return ContentLoader.CombinePath(ContentPaths.ActiveUnitFolder(ContentConstValues.FOLDER_APPS), appID, scenarioID);
        }
    }

    public void AssignAppCore(AppCore core)
    {
        appCore = core;
    }

    public void AssignContentPath(string path)
    {
        contentPath = path;
    }

    public void PresetupScenario()
    {
        for (int i = 0; i < stages.Count; i++)
            stages[i].PresetupStage(scenarioID, ScenarioPath, extension);
    }

    public Stage GetStageByID(string stageID)
    {
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].stageID == stageID)
                return stages[i];
        }

        return null;
    }

    public bool ValidateContent(out string validationMessage)
    {
        List<string> issues = new List<string>();
        issues.Add("Issues:");

        if (string.IsNullOrEmpty(appID))
            issues.Add("App ID is not set.");

        if (string.IsNullOrEmpty(scenarioID))
            issues.Add("Scenario ID is not set.");

        if (string.IsNullOrEmpty(header))
            issues.Add("Header is not set.");

        if (size.x <= 100 || size.y <= 100)
            issues.Add("Size is not set or has invalid values.");

        if (margin.x < 0 || margin.y < 0 || margin.z < 0 || margin.w < 0)
            issues.Add("Margin values cannot be negative.");

        if (stages.IsNullOrEmpty())
            issues.Add("Stages list is empty.");

        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i].ValidateContent(out string stageValidationMessage) == false)
                issues.Add($"{i}. {stageValidationMessage}");
        }

        validationMessage = string.Join("\n", issues);
        return issues.Count <= 1;
    }

    public string ToFormattedString()
    {
        return $"<b>{header}</b>\n" +
               $"<size=40%><i>{scenarioID}</i></size>\n" +
               $"<size=55%>Size: {size.x}x{size.y}</size>\n";
    }


    [Serializable]
    public class Stage : IValidateContent
    {
        public string stageID;
        public string customHeader;
        public List<Frame> frames = new List<Frame>();
        public List<Link> links = new List<Link>();

        private string scenarioID;
        private string contentPath;
        private string imagesExtension;

        public void PresetupStage(string scenarioID, string contentPath, string imagesExtension)
        {
            this.scenarioID = scenarioID;
            this.contentPath = contentPath;
            this.imagesExtension = imagesExtension;
        }

        public void LoadImages()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<string> fileNames = frames.Select(frame => $"{frame.imageName}").ToList();

            Dictionary<string, Texture2D> imageDictionary = ContentLoader.ReturnPictures(contentPath, fileNames, imagesExtension, false);

            for (int i = 0; i < frames.Count; i++)
            {
                frames[i].LoadImage(imageDictionary);
            }

            for (int i = frames.Count - 1; i >= 0; i--)
            {
                if (frames[i].Image == null)
                {
                    Debug.LogWarning($"Frame '{frames[i].imageName}' in stage '{stageID}' could not be loaded. Removing it from the list.");
                    frames.RemoveAt(i);
                }
            }

            stopwatch.Stop();
            Debug.Log($"Loaded images for AppScenario '{scenarioID}' in {stopwatch.ElapsedMilliseconds} ms.");
        }

        public void UnloadImages()
        {
            for (int i = frames.Count - 1; i >= 0; i--)
            {
                frames[i].Image.texture.Destroy();
                frames[i].Image.Destroy();
                frames[i].Image = null;
            }
        }

        public bool ValidateContent(out string validationMessage)
        {
            List<string> issues = new List<string>();
            issues.Add("Issues:");

            if (string.IsNullOrEmpty(stageID))
                issues.Add("Stage ID is not set.");

            if (frames.IsNullOrEmpty())
                issues.Add("Frames list is empty.");

            if (links.IsNullOrEmpty())
                issues.Add("Links list is empty.");

            for (int i = 0; i < links.Count; i++)
            {
                if (links[i].ValidateContent(out string linkValidationMessage) == false)
                    issues.Add($"{i}. {linkValidationMessage}");
            }

            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i].ValidateContent(out string frameValidationMessage) == false)
                    issues.Add($"{i}. {frameValidationMessage}");
            }

            validationMessage = string.Join("\n", issues);
            return issues.Count <= 1;
        }
    }

    [Serializable]
    public class Frame : IValidateContent
    {
        public string imageName;
        public int delayMs = 100;

        [DoNotSerialize]
        private Sprite image;

        [DoNotSerialize]
        public Sprite Image { get => image; set => image = value; }

        public void LoadImage(Dictionary<string, Texture2D> imageDictionary)
        {
            if (imageDictionary.TryGetValue(imageName, out Texture2D texture))
            {
                image = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            }
            else
            {
                Debug.LogWarning($"Image '{imageName}' not found in the provided dictionary.");
            }
        }

        public bool ValidateContent(out string validationMessage)
        {
            List<string> issues = new List<string>();
            issues.Add("Issues:");

            if (string.IsNullOrEmpty(imageName))
                issues.Add("Image name is not set.");

            if (delayMs <= 0)
                issues.Add("Delay must be greater than zero.");

            validationMessage = string.Join("\n", issues);
            return issues.Count <= 1;
        }
    }

    [Serializable]
    public class Link : IValidateContent
    {
        public string textToInput;
        public string nextStageID;
        public Vector2 linkPoistion;
        public Vector2 linkSize;

        public bool ValidateContent(out string validationMessage)
        {
            List<string> issues = new List<string>();
            issues.Add("Issues:");

            if (string.IsNullOrEmpty(nextStageID))
                issues.Add("Next stage ID is not set.");

            if (linkPoistion.x < 0 || linkPoistion.y < 0)
                issues.Add("Link position cannot be negative.");

            if (linkSize.x <= 0 || linkSize.y <= 0)
                issues.Add("Link size must be greater than zero.");

            validationMessage = string.Join("\n", issues);
            return issues.Count <= 1;
        }
    }
}

