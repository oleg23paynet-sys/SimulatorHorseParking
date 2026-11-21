using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using HorseParking.Presentation.Composition;
using HorseParking.Presentation.Onboarding;
using HorseParking.Presentation.Parking;
using HorseParking.Presentation.Localization;
using Object=UnityEngine.Object;
namespace HorseParking.Presentation.Editor
{
 public static class FirstPlayableInstaller
 {
  public static void Install()
  {
   var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/ParkingMvp.unity");
   var root=Object.FindAnyObjectByType<GameCompositionRoot>();var expansion=Object.FindAnyObjectByType<ParkingExpansionPresenter>();
   var capacityLabel=(Text)new SerializedObject(expansion).FindProperty("capacityText").objectReferenceValue;
   capacityLabel.transform.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(900,105);
   var queue=expansion.GetComponent<ParkingArrivalQueue>();
   if(queue==null)
   {
    queue=expansion.gameObject.AddComponent<ParkingArrivalQueue>();
    var source=GameObject.Find("ClientMountedHorseRider_01");var visitors=new GameObject[2];
    for(int i=0;i<2;i++)
    {
     var visitor=Object.Instantiate(source);visitor.name="WaitingVisitor_"+(i+1);
     foreach(var script in visitor.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
     foreach(var collider in visitor.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
     foreach(var body in visitor.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
     foreach(var animator in visitor.GetComponentsInChildren<Animator>(true)){animator.applyRootMotion=false;animator.speed=0;}
     visitor.transform.position=source.transform.position+new Vector3(3+i*2.5f,0,-2);
     visitor.transform.SetParent(expansion.transform,true);visitor.SetActive(false);visitors[i]=visitor;
    }
    queue.Configure(visitors);
   }
   var old=Object.FindAnyObjectByType<FirstMinutesTutorialPresenter>();var fields=new SerializedObject(old);
   T Ref<T>(string name) where T:Object => (T)fields.FindProperty(name).objectReferenceValue;
   var panel=Ref<GameObject>("panel");old.enabled=false;
   var objectives=old.GetComponent<FirstPlayableObjectives>()??old.gameObject.AddComponent<FirstPlayableObjectives>();
   objectives.Configure(root,panel,Ref<Text>("titleText"),Ref<Text>("instructionText"),Ref<Text>("footerText"),Ref<Image>("progressFill"));
   panel.SetActive(true);
   var rect=panel.GetComponent<RectTransform>();rect.sizeDelta=new Vector2(840,200);rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-20,20);
   void Layout(Text text,float top,float height,int fontSize)
   {var r=text.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(24,-top);r.sizeDelta=new Vector2(792,height);text.fontSize=fontSize;}
   Layout(Ref<Text>("titleText"),10,30,24);Layout(Ref<Text>("instructionText"),45,90,23);Layout(Ref<Text>("footerText"),145,42,16);
   var prompt=Object.FindAnyObjectByType<HorseParking.Presentation.Logistics.InteractionPromptHud>();
   var promptFields=new SerializedObject(prompt);promptFields.FindProperty("showIdleCartGuide").boolValue=false;promptFields.ApplyModifiedPropertiesWithoutUndo();
   var promptLabel=(Text)promptFields.FindProperty("promptText").objectReferenceValue;
   var promptRect=promptLabel.transform.parent.GetComponent<RectTransform>();promptRect.anchorMin=promptRect.anchorMax=promptRect.pivot=new Vector2(.5f,.5f);promptRect.anchoredPosition=Vector2.zero;promptRect.sizeDelta=new Vector2(700,60);
   promptLabel.rectTransform.sizeDelta=new Vector2(670,52);promptLabel.fontSize=22;
   Ref<Text>("instructionText").fontSize=23;Ref<Text>("instructionText").resizeTextForBestFit=true;Ref<Text>("instructionText").resizeTextMinSize=18;Ref<Text>("instructionText").resizeTextMaxSize=23;
   var inventory=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Settings/LogisticsInventorySettings.asset"));inventory.FindProperty("demoModeEnabled").boolValue=false;inventory.FindProperty("startingGold").intValue=10;inventory.ApplyModifiedPropertiesWithoutUndo();
   var construction=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Settings/ConstructionRequirementsSettings.asset"));construction.FindProperty("constructionDurationSeconds").floatValue=120;construction.ApplyModifiedPropertiesWithoutUndo();
   var localization=AssetDatabase.LoadAssetAtPath<GameLocalizationSettings>("Assets/_Project/Settings/GameLocalizationSettings.asset");
   void L(string key,string ru,string en){localization.EnsureTranslation("ru",key,ru);localization.EnsureTranslation("en",key,en);}
   L("firstplay.title","КОННЫЙ ДВОР • ПЕРВОЕ РАСШИРЕНИЕ","HORSE YARD • FIRST EXPANSION");
   L("firstplay.footer","WASD — движение • E / ЛКМ — действие • F1 — цели","WASD move • E / Click interact • F1 objectives");
   L("firstplay.grace","До расширения обязательных расходов нет","No recurring costs before expansion");
   L("firstplay.balance_grace","Золото: {gold} • Расходы начнутся после расширения","Gold: {gold} • Recurring costs start after expansion");
   L("firstplay.first","Обслужите первого всадника. Дождитесь его возвращения, заберите мешочек с оплатой у лошади (E / ЛКМ).","Serve the first rider. Wait for their return, then collect the payment bag beside the horse (E / Click).");
   L("firstplay.exit","Оплата получена. Откройте выход из стойла (E / ЛКМ) и дождитесь выезда лошади.","Payment received. Open the exit gate (E / Click) and wait until the horse leaves.");
   L("firstplay.earn","Заработайте на второе стойло: золото {gold}, недостающие материалы стоят {cost}. Забирайте оплату и выпускайте клиентов — место освободится после выезда.","Earn the second bay: gold {gold}, missing materials cost {cost}. Collect payments and open gates; bays become free after horses leave.");
   L("firstplay.dispatch","Отправьте телегу к магазину. Подойдите к складу справа и нажмите E. Нужно купить: {recipe}","Send the cart to the store. Approach the warehouse on the right and press E. Buy: {recipe}");
   L("firstplay.travel","Телега едет к магазину. Пока ждёте, обслуживайте парковку.","The cart is heading to the store. Serve parking visitors while waiting.");
   L("firstplay.buy","Магазин слева: подойдите и нажмите E. Докупить: {recipe} Стоимость: {cost}. Материалы загружаются в телегу.","Store on the left: approach and press E. Still needed: {recipe} Cost: {cost}. Purchases go into the cart.");
   L("firstplay.return","Все материалы куплены. У магазина нажмите E и отправьте телегу обратно на склад.","Materials purchased. Press E at the store and send the cart back to the warehouse.");
   L("firstplay.returning","Доставка едет на склад. После прибытия выгрузите груз — материалы в телеге ещё не доступны стройке.","Delivery is returning. Unload after arrival: cargo in the cart is not yet available for construction.");
   L("firstplay.unload","Телега на складе. Подойдите к грузу и разгрузите его (E / ЛКМ). Затем идите к табличке стройки.","Cart at warehouse. Approach its cargo and unload (E / Click), then visit the construction sign.");
   L("firstplay.build","Материалы на складе. У таблички стройки нажмите E, затем «Начать строительство». Вместимость увеличится с 1 до 2.","Materials delivered. Press E at the construction sign, then Start construction. Capacity increases from 1 to 2.");
   L("firstplay.building","Гоблины строят второе стойло. Они работают самостоятельно; обслуживайте клиентов на первом месте.","Goblins are building the second bay automatically. Keep serving the first bay.");
   L("firstplay.two","Второе стойло открыто! Обслужите ещё двух посетителей: {served}/2. У каждого своя оплата и выход.","Second bay open! Serve two more visitors: {served}/2. Each has a separate payment and exit.");
   L("firstplay.complete","Первый этап пройден: двор расширен, новые посетители обслужены. Можно продолжать зарабатывать. Замковые заказы появятся позже.","First stage complete: yard expanded and new visitors served. Keep earning; castle contracts are a future feature.");
   L("firstplay.queue","У въезда: {waiting}/2 • Уехали из-за полной очереди: {lost}","Waiting at entrance: {waiting}/2 • Turned away: {lost}");
   L("ui.cart.guide.warehouse","ТЕЛЕГА У СКЛАДА — каменный дом справа. Подойдите к складу и нажмите E, чтобы отправить телегу в магазин.","CART AT WAREHOUSE — stone house on the right. Approach and press E to dispatch to the store.");
   L("ui.cart.guide.store","ТЕЛЕГА У МАГАЗИНА — каменный дом слева. Нажмите E у магазина для покупки и отправки доставки.","CART AT STORE — stone house on the left. Press E to buy materials and dispatch delivery.");
   L("ui.cart_dispatch.instruction.ready_warehouse","Телега у склада. Отправьте её в магазин, чтобы купить материалы.","Cart at the warehouse. Send it to the store to purchase materials.");
   L("ui.cart_dispatch.instruction.go_warehouse","Подойдите к складу — каменному дому справа — и нажмите E.","Approach the warehouse, the stone house on the right, and press E.");
   L("ui.cart_dispatch.instruction.go_store","Подойдите к магазину — каменному дому слева — и нажмите E.","Approach the store, the stone house on the left, and press E.");
   EditorUtility.SetDirty(localization);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);Debug.Log("FIRST_PLAYABLE_INSTALLED");
  }
 }
}
