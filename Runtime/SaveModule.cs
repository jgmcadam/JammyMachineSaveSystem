using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JammyMachine.SaveSystem
{
    public class VersionedRawSaveData
    {
        public int SaveVersion; 
        public string SaveData;
    }

    public interface ISerializer
    {
        public bool TryDeserialize<T>(string serializedDataObject, out T deserializedDataObject);
        public bool TrySerialize<T>(T dataObject, out string serializedDataObject);
    }

    public abstract class SaveDataUpgradeHandler<T> where T : class
    {
        public abstract int CurrentVersion { get; }
        public abstract int UpgradedSaveVersion { get; }
        
        public abstract bool CanUpgradeToVersion(T existingObject);
        public abstract bool TryUpgradeToVersion(T existingObject);
    }
    
    public abstract class SaveModule<T> where T:class
    {
        protected abstract int CurrentVersion { get; }
        protected abstract List<SaveDataUpgradeHandler<T>> SaveDataUpgradeHandlers { get; }
        
        private ISerializer m_serializer;

        public SaveModule(ISerializer serializer)
        {
            m_serializer = serializer;
        }

        public bool TryGetSaveDataFrom(VersionedRawSaveData versionedRawSaveData, out T saveObject)
        {
            if(!m_serializer.TryDeserialize(versionedRawSaveData.SaveData, out saveObject))
            {
                return false;
            }

            var saveVersion = versionedRawSaveData.SaveVersion;
            var upgradeHandlers = SaveDataUpgradeHandlers.OrderBy(
                vUO => vUO.CurrentVersion);
            
            foreach (var upgradeHandler in upgradeHandlers)
            {
                if (upgradeHandler.CurrentVersion == saveVersion && upgradeHandler.CanUpgradeToVersion(saveObject))
                {
                    if (upgradeHandler.TryUpgradeToVersion(saveObject))
                    {
                        saveVersion = upgradeHandler.UpgradedSaveVersion;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
                
            return CurrentVersion == saveVersion;
        }
        
        public bool TrySetSaveDataFrom(T saveObject, out VersionedRawSaveData versionedRawSaveData)
        {
            if(!m_serializer.TrySerialize(saveObject, out var saveObjectString))
            {
                versionedRawSaveData = null;
                return false;
            }

            versionedRawSaveData = new VersionedRawSaveData()
            {
                SaveVersion = CurrentVersion,
                SaveData = saveObjectString
            };
            return true;
        }
    }
}
