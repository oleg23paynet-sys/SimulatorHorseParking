using System.Collections.Generic;
using UnityEngine;
namespace HorseParking.Presentation.Parking
{
    public sealed class ParkingArrivalQueue : MonoBehaviour
    {
        [SerializeField] private GameObject[] waitingVisitors = System.Array.Empty<GameObject>();
        [SerializeField] private float arrivalInterval = 20f;
        [SerializeField] private float patienceSeconds = 60f;
        private readonly Queue<float> deadlines = new();
        private float nextArrival;
        public int Waiting => deadlines.Count;
        public int TurnedAway { get; private set; }
        public int Expired { get; private set; }
        public int Admitted { get; private set; }
        public float NextArrivalSeconds => Mathf.Max(0, nextArrival-Time.time);
        public float FirstVisitorPatience => deadlines.Count==0?0:Mathf.Max(0,deadlines.Peek()-Time.time);
        public void Configure(GameObject[] visitors) => waitingVisitors=visitors;
        private void Awake(){deadlines.Enqueue(Time.time+patienceSeconds);nextArrival=Time.time+arrivalInterval;}
        private void Start()=>RefreshVisuals();
        private void Update()
        {
            RemoveExpired();
            if(Time.time>=nextArrival)
            {
                nextArrival=Time.time+Mathf.Max(1,arrivalInterval);
                if(Waiting<2)deadlines.Enqueue(Time.time+Mathf.Max(1,patienceSeconds));else TurnedAway++;
            }
            RefreshVisuals();
        }
        private void RemoveExpired()
        {
            while(deadlines.Count>0&&deadlines.Peek()<=Time.time){deadlines.Dequeue();Expired++;TurnedAway++;}
        }
        public bool TryAdmit()
        {
            RemoveExpired();if(Waiting==0)return false;
            deadlines.Dequeue();Admitted++;RefreshVisuals();return true;
        }
        private void RefreshVisuals()
        {
            for(int i=0;i<waitingVisitors.Length;i++)if(waitingVisitors[i]!=null)waitingVisitors[i].SetActive(i<Waiting);
        }
    }
}
