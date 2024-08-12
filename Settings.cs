using System;
using UnityEngine;

namespace GameServices
{
    [Serializable]
    internal class Settings
    {
        [Tooltip("Automatically initialized all game services when the game starts")]
        public bool autoInitializeServices = false;

        [Header("Logging")] 
        public bool logMessages = true;
        public bool logWarnings = true;
        public bool logErrors = true;
    }
}