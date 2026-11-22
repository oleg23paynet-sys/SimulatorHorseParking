using System.Linq;
using UnityEngine;

namespace HorseParking.Presentation.Parking
{
    // Reuse the scene's existing rail meshes. Enclose the actual horse parking
    // point, rather than treating disconnected decorative fences as a stall.
    internal static class ParkingStallLayout
    {
        public static void Prepare(ParkingMvpRuntimeController runtime, Transform source)
        {
            var center = runtime.Route.ParkingPosition;
            var names = new[] { "LeftFence", "RightFence", "BackFence" };
            var offsets = new[] { new Vector3(-2.1f,0,0), new Vector3(2.1f,0,0), new Vector3(0,0,2.5f) };
            var sizes = new[] { new Vector3(.18f,1.35f,5), new Vector3(.18f,1.35f,5), new Vector3(4.38f,1.35f,.18f) };
            var colliders = new System.Collections.Generic.List<Collider>();
            for(int i=0;i<names.Length;i++)
            {
                string name="ParkingSlot_01_"+names[i];
                var fence=source!=null?source.Find(name)?.gameObject:GameObject.Find(name);
                if(fence==null)continue;
                var renderers=fence.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
                if(renderers.Length==0)continue;
                var bounds=renderers[0].bounds;
                foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                // A side rail must run along the horse's length, the rear rail across it.
                bool alongX=bounds.size.x>bounds.size.z;
                if(alongX!=(i==2))
                {
                    fence.transform.RotateAround(bounds.center,Vector3.up,90);
                    bounds=renderers[0].bounds;
                    foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                }
                var holder=new GameObject(name).transform;
                holder.SetParent(source,true);
                holder.position=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                fence.name=name+"_Mesh";
                fence.transform.SetParent(holder,true);
                foreach(var collider in fence.GetComponentsInChildren<Collider>(true))collider.enabled=false;
                holder.localScale=new Vector3(sizes[i].x/Mathf.Max(.01f,bounds.size.x),sizes[i].y/Mathf.Max(.01f,bounds.size.y),sizes[i].z/Mathf.Max(.01f,bounds.size.z));
                holder.position=center+offsets[i];
                var barrier=holder.gameObject.AddComponent<BoxCollider>();
                barrier.center=new Vector3(0,bounds.size.y*.5f,0);
                barrier.size=bounds.size;
                var obstacle=holder.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;
                obstacle.center=barrier.center;
                obstacle.size=barrier.size;
                obstacle.carving=true;
                colliders.Add(barrier);
            }
            runtime.SetStallObstacles(colliders.ToArray());
        }
    }
}
