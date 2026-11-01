using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BuildScripts
{
    public class ChangeGameVersionNumber
    {
        public static void ChangeVersionNumber(int position, int change)
        {
            try
            {
                if (change != -1 && change != 1)
                    throw new Exception("Incorrect 'change' value");

                if (position >= 0)
                    throw new Exception("Position must be negative");

                string versionString = PlayerSettings.bundleVersion;
                string[] versionBaseElements = versionString.Split(":");
                versionString = versionBaseElements[0];
                string buildNumber = versionBaseElements[1];

                string[] versionElements = versionString.Split(".");

                int targetPosition = versionElements.Length + position;
                int targetNumber = int.Parse(versionElements[targetPosition]);
                targetNumber += change;

                versionElements[targetPosition] = targetNumber.ToString();
                versionString = string.Join(".", versionElements);

                PlayerSettings.bundleVersion = $"{versionString}:{buildNumber}";
            }
            catch (Exception ex)
            {
                Debug.LogError("Cannot increase version number.\n" + ex.Message);
            }
        }
    }
}
