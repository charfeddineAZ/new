# تحسينات 2026 - Fantastic City Generator

## واجهة المحرر
- دعم حفظ إعدادات الجلسة تلقائيًا (`EditorPrefs`) واستعادتها عند فتح الأداة.
- دعم تصدير/استيراد Presets كملفات JSON.
- زر إنشاء ملف `Runtime Profile` جاهز للاستخدام داخل المشهد.
- زر `Quick Random` لتوليد إعداد عشوائي سريع للمدن التابعة.
- عرض ملخص آخر عملية توليد (الحجم، عدد المدن التابعة، عدد وصلات الطرق، الزمن).
- عرض تحذيرات إعدادات قبل التوليد (مثل `Custom` بدون نقاط فعالة).

## توليد المدن التابعة
- أوضاع توزيع: `Preset`, `Random`, `Custom`.
- دعم عدد مدن تابعـة حتى 64.
- دعم `Global Offset` لجميع المدن التابعة.
- دعم Seed ثابت للوضع العشوائي.
- دعم أنماط ربط متقدمة:
  - `MainOnly`
  - `Chain`
  - `Nearest`
  - `FullMesh`
- دعم `Nearest Links`, `Close Loop`, وتحديد طول قطعة الطريق.

## خطوط التوليد (Pipeline)
- زر `Generate Full Pipeline`:
  - توليد الشوارع
  - توليد المباني
  - تحديث/إضافة نظام المرور
- خيارات أتمتة:
  - `Auto Generate Buildings`
  - `Auto Add Traffic`

## Runtime
- إعادة بناء `RunTimeSample` بالكامل بإعدادات متقدمة مشابهة لواجهة المحرر.
- دعم التوليد الكامل من Runtime عبر `GenerateFullPipeline()`.
- دعم التطبيق من `CityGenerationProfile`.
- دعم `SetNight(bool)` لتبديل يوم/ليل برمجيًا.
- دعم `CityGenerationRequest` كنموذج موحد للطلبات وتشغيله مباشرة في `CityGenerator`.
- إضافة `RuntimeGenerationHotkeys`:
  - `F5` توليد كامل
  - `F6` تحديث المرور
  - `F7` تبديل يوم/ليل

## الاستقرار والأداء
- تحسين `TrafficSystem`:
  - منع التكرار غير المقصود لـ `InvokeRepeating`.
  - حماية عند غياب سيارات `IaCars`.
  - تحسين استخدام `GetComponent` وتجنب التكرار.
  - حماية عند غياب نقاط Spawn.
  - إزالة اعتماد غير مطلوب على `UnityEditor` داخل Runtime Script.
- تحسين `FreeCamera`:
  - حركة عمودية (`Q/E`).
  - تكبير/تصغير سرعة الكاميرا بعجلة الفأرة.
  - خيار قفل المؤشر أثناء النظر.
- تحسين `FPSDisplay`:
  - تصحيح ألوان الأداء.
  - إضافة عرض `ms`.
  - حماية من القسمة على صفر.
- تحسين `DayNight`:
  - كاش داخلي لتحويل المواد Day/Night.
  - حماية من اختلاف أطوال مصفوفات المواد.
  - منع أخطاء `MeshRenderer` عبر التعامل مع `Renderer` مباشرة.

## إدارة الإعدادات
- دعم Profile نشط داخل واجهة المحرر:
  - تطبيق الإعدادات من Profile إلى الواجهة.
  - حفظ الإعدادات الحالية داخل Profile.
- فحص صحة المشروع (`Health Check`) مع تقرير Console.
- إحصائيات التوليد (`GenerationStats`) ومعاينة الملخص مباشرة داخل النافذة.

## 2026 Wave 2 - City Network + Runtime Control
- Added a generated city network graph in `CityGenerator`:
  - Nodes: main city + all generated satellite cities.
  - Links: main-to-satellite and satellite-to-satellite connections.
  - Runtime access via:
    - `GetLastGenerationNetworkSummary()`
    - `GetLastGenerationNetworkClone()`
- Added generation event hook:
  - `OnGenerationCompleted(GenerationStats, GenerationNetwork)`
- Added automatic scene anchors after generation:
  - Root: `Generated-City-Network` under `City-Maker`.
  - Nodes as child GameObjects with `GeneratedCityAnchor` component.
- Added optional debug line rendering for links (runtime + editor configurable):
  - `createCityAnchors`
  - `createConnectionDebugLines`
  - `connectionDebugLineHeight`
- Unified these new network options across:
  - `CityGenerationProfile`
  - `CityGenerationRequest`
  - `RunTimeSample`
  - `FCityGenerator` editor window.
- Added full runtime control panel script:
  - `RuntimeGenerationControlUI`
  - Includes full generation pipeline controls, satellite network controls, night/day toggle, and JSON preset save/load in `Application.persistentDataPath`.
