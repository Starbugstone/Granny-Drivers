using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrannyRacer.Racing
{
    [Serializable] public sealed class CircuitDestination
    {
        public string title;
        public string scenePath;
        public string description;
    }
    public sealed class CircuitTour : MonoBehaviour
    {
        [SerializeField] private CircuitDestination[] destinations=Array.Empty<CircuitDestination>();
        private bool loading;
        public CircuitDestination[] Destinations=>destinations;
        public bool IsLoading=>loading;
        public void Configure(CircuitDestination[] entries)=>destinations=entries;
        public bool Travel(int index)
        {
            if(loading || index<0 || index>=destinations.Length || !Application.CanStreamedLevelBeLoaded(destinations[index].scenePath))return false;
            loading=true;Time.timeScale=1;
            SceneManager.LoadSceneAsync(destinations[index].scenePath);
            return true;
        }
    }
}
