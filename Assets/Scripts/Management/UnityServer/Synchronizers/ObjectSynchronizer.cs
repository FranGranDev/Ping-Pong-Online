using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Management
{
    public class ObjectSynchronizer : MonoBehaviour
    {
        private Dictionary<string, NetworkObject> localObjects;
        private Dictionary<string, NetworkObject> remoteObjects;

        public event System.Action<string, object> OnObjectUpdated;

        public void Initialize()
        {
            IEnumerable<NetworkObject> objects = transform.GetComponentsInChildren<NetworkObject>(true);

            localObjects = objects
                .Where(x => x.Mine)
                .ToDictionary(x => x.Id);

            remoteObjects = objects
                .Where(x => !x.Mine)
                .ToDictionary(x => x.Id);

            localObjects
                .Values
                .ToList()
                .ForEach(x => x.OnUpdated += CallOnObjectUpdated);
        }

        private void CallOnObjectUpdated(string id, object data)
        {
            OnObjectUpdated?.Invoke(id, data);
        }

        public void UpdateRemoteObject(string id, object data)
        {
            if(remoteObjects.ContainsKey(id))
            {
                remoteObjects[id].Synchronize(data);
            }
        }
    }
}
