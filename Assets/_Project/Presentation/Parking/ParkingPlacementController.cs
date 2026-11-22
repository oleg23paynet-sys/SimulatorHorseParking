using System.Linq;
using HorseParking.Core.Construction;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Construction;
using HorseParking.Presentation.Player;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
namespace HorseParking.Presentation.Parking
{
    /// <summary>Runtime placement; no editor scene generation or player build is required.</summary>
    public sealed class ParkingPlacementController : MonoBehaviour
    {
        private ParkingExpansionPresenter owner;
        private GameCompositionRoot root;
        private GameObject template, ghost;
        private ConstructionProcessPresenter construction;
        private FirstPersonPlayerController player;
        private Camera playerCamera, placementCamera;
        private Material ghostMaterial;
        private Bounds footprint;
        private Vector3 candidate, groundPoint, cameraOrigin;
        private Quaternion rotation=Quaternion.identity;
        private bool valid, helpVisible, originalCameraEnabled;
        private float nextValidation, inputReadyAt;
        private float noticeUntil;
        private string notice;
        private int releaseInputAfterFrame=-1;
        private Vector2 helpScroll;
        private float PlacementPanelHeight => Mathf.Min(160, Screen.height * .4f);
        private string message="B — выбрать место нового стойла. H — справка по парковке.";
        public bool IsPlacing { get; private set; }
        public void Configure(ParkingExpansionPresenter manager,GameCompositionRoot composition,GameObject source,ConstructionProcessPresenter process)
        {owner=manager;root=composition;template=source;construction=process;}
        public void BeginPlacement()
        {
            if(IsPlacing)return;
            if(!owner.PrepareProject()) {notice="Сначала дождитесь окончания стройки и ухода рабочих.";noticeUntil=Time.unscaledTime+4;return;}
            player=FindAnyObjectByType<FirstPersonPlayerController>();
            playerCamera=player!=null?player.ViewCamera:null;
            if(playerCamera==null){message="Не найдена камера игрока.";return;}
            CreateGhost();rotation=Quaternion.identity;valid=false;
            originalCameraEnabled=playerCamera.enabled;playerCamera.enabled=false;player.SetUiInputBlocked(true);
            placementCamera=new GameObject("ParkingPlacementCamera").AddComponent<Camera>();
            placementCamera.orthographic=true;placementCamera.orthographicSize=18;placementCamera.farClipPlane=180;
            cameraOrigin=player.transform.position;placementCamera.transform.SetPositionAndRotation(cameraOrigin+Vector3.up*40,Quaternion.Euler(90,0,0));
            IsPlacing=true;inputReadyAt=Time.unscaledTime+.25f;
        }
        private void CreateGhost()
        {
            if(ghost!=null)Destroy(ghost);
            if(ghostMaterial!=null)Destroy(ghostMaterial);
            ghost=new GameObject("ParkingPlacementPreview");
            ghostMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            ghostMaterial.SetFloat("_Surface",1);ghostMaterial.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);ghostMaterial.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            ghostMaterial.SetFloat("_ZWrite",0);ghostMaterial.SetOverrideTag("RenderType","Transparent");ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");ghostMaterial.renderQueue=3000;
            bool hasBounds=false;
            foreach(Transform part in template.transform)
            {
                if(!part.name.Contains("Fence")&&!part.name.StartsWith("ExitGate"))continue;
                foreach(var source in part.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer=source.GetComponent<MeshRenderer>();
                    if(renderer==null||!renderer.enabled||source.sharedMesh==null)continue;
                    var piece=new GameObject(source.name,typeof(MeshFilter),typeof(MeshRenderer));piece.transform.SetParent(ghost.transform,false);
                    piece.transform.localPosition=template.transform.InverseTransformPoint(source.transform.position);
                    piece.transform.localRotation=Quaternion.Inverse(template.transform.rotation)*source.transform.rotation;
                    var scale=template.transform.lossyScale;var size=source.transform.lossyScale;
                    piece.transform.localScale=new Vector3(size.x/scale.x,size.y/scale.y,size.z/scale.z);
                    piece.GetComponent<MeshFilter>().sharedMesh=source.sharedMesh;
                    var target=piece.GetComponent<MeshRenderer>();target.sharedMaterials=Enumerable.Repeat(ghostMaterial,source.sharedMesh.subMeshCount).ToArray();target.shadowCastingMode=ShadowCastingMode.Off;
                    if(!hasBounds){footprint=target.bounds;hasBounds=true;}else footprint.Encapsulate(target.bounds);
                }
            }
            if(!hasBounds)footprint=new Bounds(new Vector3(0,1,0),new Vector3(6,2,8));
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(floor.GetComponent<Collider>());floor.name="ReservedFootprint";floor.transform.SetParent(ghost.transform,false);
            floor.transform.localPosition=new Vector3(footprint.center.x,footprint.min.y+.025f,footprint.center.z);floor.transform.localScale=new Vector3(footprint.size.x,.035f,footprint.size.z);
            floor.GetComponent<Renderer>().sharedMaterial=ghostMaterial;floor.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        private void Update()
        {
            if(releaseInputAfterFrame>=0&&Time.frameCount>releaseInputAfterFrame)
            {releaseInputAfterFrame=-1;if(player!=null)player.SetUiInputBlocked(false);}
            if(Input.GetKeyDown(KeyCode.H))helpVisible=!helpVisible;
            if(!IsPlacing)return;
            if(Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1)){Cancel();return;}
            var pan=new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"))*18*Time.unscaledDeltaTime;
            placementCamera.transform.position+=pan;
            var offset=placementCamera.transform.position-cameraOrigin;offset.x=Mathf.Clamp(offset.x,-45,45);offset.z=Mathf.Clamp(offset.z,-45,45);placementCamera.transform.position=cameraOrigin+offset;
            placementCamera.orthographicSize=Mathf.Clamp(placementCamera.orthographicSize-Input.mouseScrollDelta.y*2,8,30);
            if(Input.GetKeyDown(KeyCode.R)){rotation*=Quaternion.Euler(0,90,0);nextValidation=0;}
            if(Time.unscaledTime>=nextValidation)
            {
                nextValidation=Time.unscaledTime+.1f;
                var ray=placementCamera.ScreenPointToRay(Input.mousePosition);
                var hits=Physics.RaycastAll(ray,160,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance);
                bool found=false;
                foreach(var hit in hits)
                {
                    if(!(hit.collider is TerrainCollider))continue;
                    found=true;groundPoint=hit.point;
                    candidate=groundPoint-rotation*new Vector3(footprint.center.x,footprint.min.y,footprint.center.z);
                    ghost.transform.SetPositionAndRotation(candidate,rotation);
                    valid=Validate();break;
                }
                if(!found){valid=false;message="Наведите макет на землю внутри игровой площадки.";}
                ghostMaterial.SetColor("_BaseColor",valid?new Color(.15f,1,.4f,.32f):new Color(1,.15f,.12f,.35f));
            }
            if(Time.unscaledTime<inputReadyAt||!Input.GetMouseButtonDown(0))return;
            if(UnityEngine.EventSystems.EventSystem.current!=null&&UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())return;
            if(Input.mousePosition.y<PlacementPanelHeight+20)return;
            if(!valid||!Validate())return;
            if(!root.ConstructionRequirementsUseCase.GetSnapshot().CanStart){notice="Не хватает материалов НА СКЛАДЕ. Отмена: ПКМ/Esc; доставьте груз и попробуйте снова.";noticeUntil=Time.unscaledTime+4;return;}
            TryPlaceAt(groundPoint,rotation);
        }
        public bool TryPlaceAt(Vector3 surfacePoint, Quaternion orientation)
        {
            if(!IsPlacing)return false;
            groundPoint=surfacePoint;rotation=orientation;
            candidate=groundPoint-rotation*new Vector3(footprint.center.x,footprint.min.y,footprint.center.z);
            ghost.transform.SetPositionAndRotation(candidate,rotation);
            if(!Validate()||!root.ConstructionRequirementsUseCase.GetSnapshot().CanStart)return false;
            if(owner.ConfirmSite(candidate,rotation,groundPoint))
            {
                ghostMaterial.SetColor("_BaseColor",new Color(1,.7f,.15f,.25f));
                ExitCamera();message="Площадка закреплена. Гоблины строят; новое место откроется после их ухода.";
                return true;
            }
            return false;
        }
        private bool Blocks(Collider collider)
        {
            if(collider.isTrigger||collider is TerrainCollider)return false;
            if(ghost!=null&&collider.transform.IsChildOf(ghost.transform))return false;
            if(construction!=null&&collider.transform.IsChildOf(construction.transform))return false;
            return collider.enabled&&collider.gameObject.activeInHierarchy;
        }
        private bool Validate()
        {
            var center=candidate+rotation*footprint.center;
            var half=footprint.extents;half.y=Mathf.Max(.25f,half.y-.1f);half.x+=.3f;half.z+=.3f;
            if(Physics.OverlapBox(center+Vector3.up*.15f,half,rotation,~0,QueryTriggerInteraction.Ignore).Any(Blocks))
            {message="Красный: занято. Отступите от зданий, деревьев, лошадей и других стоил.";return false;}
            foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
            {
                var corner=groundPoint+rotation*new Vector3(x*half.x,0,z*half.z);
                if(!NavMesh.SamplePosition(corner,out var nav,.65f,NavMesh.AllAreas)||Mathf.Abs(nav.position.y-groundPoint.y)>.25f)
                {message="Нужна ровная площадка внутри доступной зоны, без склона.";return false;}
            }
            // Existing routes are reservations too, even when there is no horse on them.
            foreach(var slot in owner.Slots)
            {
                var path=slot.GetAccessRoute();
                for(int segment=1;segment<path.Length;segment++)
                {
                    int samples=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(path[segment-1],path[segment])/.4f));
                    for(int sample=0;sample<=samples;sample++)
                    {
                        var local=Quaternion.Inverse(rotation)*(Vector3.Lerp(path[segment-1],path[segment],sample/(float)samples)-center);
                        if(Mathf.Abs(local.x)<half.x+.65f&&Mathf.Abs(local.z)<half.z+.65f)
                        {message="Здесь проходит маршрут другого стойла. Оставьте въезд и выезд свободными.";return false;}
                    }
                }
            }
            Transform Point(string name)=>template.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
            Vector3 World(Transform point)=>candidate+rotation*template.transform.InverseTransformPoint(point.position);
            var entranceQueue=owner.GetComponent<ParkingArrivalQueue>();
            var entry=Point("ClientEntryLanePoint_01");
            if(entranceQueue!=null && entry!=null)
            {
                var relative=Quaternion.Inverse(rotation)*(entranceQueue.EntrancePosition-center);
                if(Mathf.Abs(relative.x)<half.x+3 && Mathf.Abs(relative.z)<half.z+3)
                {message="Оставьте общий въезд и место для очереди свободными.";return false;}
                var access=new NavMeshPath();
                if(!NavMesh.SamplePosition(entranceQueue.EntrancePosition,out var start,2f,NavMesh.AllAreas)
                    || !NavMesh.SamplePosition(World(entry),out var end,1f,NavMesh.AllAreas)
                    || !NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,access)
                    || access.status!=NavMeshPathStatus.PathComplete)
                {message="От общей очереди нет проезда к этому стойлу. Выберите доступное место.";return false;}
            }
            var names=new[]{"ClientEntryLanePoint_01","ClientParkingPoint_01","ClientEntryLanePoint_01","ClientPaymentPoint_01","ClientExitPoint_01"};
            for(int i=1;i<names.Length;i++)
            {
                var a=Point(names[i-1]);var b=Point(names[i]);if(a==null||b==null){message="В шаблоне отсутствуют точки маршрута.";return false;}
                var from=World(a)+Vector3.up;var to=World(b)+Vector3.up;var delta=to-from;
                if(Physics.SphereCastAll(from,.55f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>Blocks(h.collider)))
                {message="Перекрыт подъезд или выезд лошади. Оставьте свободный коридор.";return false;}
            }
            var walking=new[]{"RiderMountPoint_01","RiderOpenLanePoint_01","RiderAwayPoint_01"};
            for(int i=1;i<walking.Length;i++)
            {
                var a=Point(walking[i-1]);var b=Point(walking[i]);if(a==null||b==null){message="Отсутствует пешеходный маршрут всадника.";return false;}
                var from=World(a)+Vector3.up;var to=World(b)+Vector3.up;var delta=to-from;
                if(Physics.SphereCastAll(from,.35f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>Blocks(h.collider)))
                {message="Перекрыт пешеходный маршрут всадника. Сдвиньте стойло.";return false;}
            }
            if(construction==null||!construction.CanPlaceParkingSite(groundPoint,rotation))
            {message="Гоблины не смогут подойти. Выберите место ближе к свободной площадке.";return false;}
            message="Зелёный: место свободно. ЛКМ — подтвердить и списать материалы.";return true;
        }
        private void ExitCamera()
        {
            IsPlacing=false;
            if(placementCamera!=null)Destroy(placementCamera.gameObject);
            if(playerCamera!=null)playerCamera.enabled=originalCameraEnabled;
            // Do not let the confirmation/cancel click also interact in first-person this frame.
            releaseInputAfterFrame=Time.frameCount;
        }
        private void Cancel(){ExitCamera();CompleteSite();message="Размещение отменено. Материалы не списаны.";}
        public void CompleteSite(){if(ghost!=null)Destroy(ghost);if(ghostMaterial!=null)Destroy(ghostMaterial);}
        private void OnDisable(){if(IsPlacing)ExitCamera();if(releaseInputAfterFrame>=0&&player!=null)player.SetUiInputBlocked(false);releaseInputAfterFrame=-1;CompleteSite();}
        private void OnGUI()
        {
            var style=new GUIStyle(GUI.skin.label){wordWrap=true,fontSize=16};
            if(IsPlacing)
            {
                style.fontSize=Mathf.Clamp(Screen.height/35,11,16);
                var rect=new Rect(20,Screen.height-PlacementPanelHeight-20,Mathf.Min(Screen.width-40,850),PlacementPanelHeight);GUI.Box(rect,GUIContent.none);
                string stock=string.Join(" • ",root.ConstructionRequirementsUseCase.GetSnapshot().Requirements.Select(r=>r.ResourceId.Value+" "+r.AvailableQuantity+"/"+r.RequiredQuantity));
                GUI.Label(new Rect(rect.x+12,rect.y+6,rect.width-24,rect.height-12),"РАЗМЕЩЕНИЕ СТОЙЛА\nWASD — камера • Колесо — масштаб • R — поворот на 90°\nЛКМ — разместить • ПКМ / Esc — отмена без расхода материалов\nСклад: "+stock+"\n"+(Time.unscaledTime<noticeUntil?notice:message),style);return;
            }
            if(!helpVisible)
            {
                var hintStyle=new GUIStyle(GUI.skin.box){fontSize=12,wordWrap=true};
                GUI.Box(new Rect(12,Screen.height-42,Mathf.Min(520,Screen.width-24),30),
                    Time.unscaledTime<noticeUntil?notice:"B — строить   H — справка   F1 — цели   I — склад   M — экономика",hintStyle);
                return;
            }
            var queue=GetComponent<ParkingArrivalQueue>();
            string text="ПАРКОВКА — ВРЕМЕННАЯ СПРАВКА (H — скрыть)\n"+
                "E / ЛКМ: поговорить, забрать мешочек с оплатой.\nПосле оплаты всадник выезжает автоматически через 1 с. Место освободится после полного выезда.\nПосле выезда — пауза 3 с; затем первый из очереди заедет автоматически.\nНет мест? Соберите оплату и дождитесь выезда готовых клиентов, ждите окончания стоянки или нажмите B и постройте ещё стойло.\nМатериалы: магазин → телега → разгрузка на складе. Материалы в телеге не считаются складскими.\nB — новая стройка, R — поворот макета, Esc/ПКМ — отмена. F1 — цели, M — экономика.\n";
            if(queue!=null)text+=$"Очередь {queue.Waiting}/2; новый посетитель через {Mathf.CeilToInt(queue.NextArrivalSeconds)} с. Первый ждёт ещё {Mathf.CeilToInt(queue.FirstVisitorPatience)} с; затем уйдёт.\n";
            int index=1;foreach(var slot in owner.Slots)text+=$"Место {index++}: {slot.ParkingStatus}\n";
            text+=message;
            float width=Mathf.Min(590,Screen.width*.48f);
            float top=Mathf.Min(180,Screen.height*.25f);
            float contentHeight=style.CalcHeight(new GUIContent(text),width-44)+24;
            float height=Mathf.Min(Screen.height-top-20,contentHeight);
            var oldColor=GUI.color;GUI.color=new Color(.03f,.03f,.03f,.96f);GUI.DrawTexture(new Rect(18,top,width,height),Texture2D.whiteTexture);GUI.color=oldColor;
            helpScroll=GUI.BeginScrollView(new Rect(18,top,width,height),helpScroll,new Rect(0,0,width-20,contentHeight));
            GUI.Label(new Rect(12,12,width-44,contentHeight-24),text,style);
            GUI.EndScrollView();
        }
    }
}
