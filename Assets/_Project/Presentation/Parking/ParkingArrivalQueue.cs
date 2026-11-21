using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    /// <summary>Single arrival stream shared by both bays. Admission reserves a bay before movement starts.</summary>
    public sealed class ParkingArrivalQueue : MonoBehaviour
    {
        [SerializeField] private GameObject[] waitingVisitors = System.Array.Empty<GameObject>();
        [SerializeField] private float arrivalInterval = 20f;
        private float nextArrival;
        public int Waiting { get; private set; } = 1;
        public int TurnedAway { get; private set; }
        public int Admitted { get; private set; }
        public void Configure(GameObject[] visitors) => waitingVisitors = visitors;
        private void Start() { nextArrival = Time.time + arrivalInterval; RefreshVisuals(); }
        private void Update()
        {
            if (Time.time < nextArrival) return;
            nextArrival = Time.time + arrivalInterval;
            if (Waiting < 2) Waiting++; else TurnedAway++;
            RefreshVisuals();
        }
        public bool TryAdmit()
        {
            if (Waiting == 0) return false;
            Waiting--; Admitted++; RefreshVisuals(); return true;
        }
        private void RefreshVisuals()
        {
            for (int i = 0; i < waitingVisitors.Length; i++)
                if (waitingVisitors[i] != null) waitingVisitors[i].SetActive(i < Waiting);
        }
    }
}
