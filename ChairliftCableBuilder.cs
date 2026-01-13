using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ChairliftCableWindow : EditorWindow
{
    private LineRenderer cableA;
    private LineRenderer cableB;

    private List<Transform> wheelAssemblies = new List<Transform>();

    private bool includeInactiveChildren = true;
    private bool sortRollers = true;
    private RollerSortMode sortMode = RollerSortMode.LocalX;

    private bool buildSecondCable = true;
    private Vector3 secondCableOffset = new Vector3(-1.5f, 0f, 0f);

    // sag
    private bool enableSag = true;
    private int subdivisionsPerSpan = 16;
    private float sagMeters = 2.0f;

    // chairs
    private GameObject chairPrefab;
    private Transform chairsParentA;
    private Transform chairsParentB;

    private bool autoGenerateChairsAfterBuild = true;

    private bool generateChairsOnA = true;
    private bool generateChairsOnB = true;

    private float chairSpacing = 12f;
    private float chairStartOffsetA = 6f;
    private float chairStartOffsetB = 6f;

    // drop the chair below the cable
    private Vector3 chairLocalOffset = new Vector3(0f, -4f, 0f);

    private bool keepChairsUpright = true;

    // if prefab faces the wrong way, toggle this
    private bool chairPrefabFacesBackward = false;

    // b faces backwards relative to travel direction
    private bool cableBFacesBackward = true;

    private enum RollerSortMode { LocalX, LocalY, LocalZ }

    [MenuItem("Tools/Chairlift Builder")]
    public static void Open()
    {
        GetWindow<ChairliftCableWindow>("chairlift cable");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("cables", EditorStyles.boldLabel);
        cableA = (LineRenderer)EditorGUILayout.ObjectField("cable A", cableA, typeof(LineRenderer), true);
        cableB = (LineRenderer)EditorGUILayout.ObjectField("cable B", cableB, typeof(LineRenderer), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("wheel assemblies (drag in order)", EditorStyles.boldLabel);

        int removeIndex = -1;

        for (int i = 0; i < wheelAssemblies.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            wheelAssemblies[i] = (Transform)EditorGUILayout.ObjectField(
                "tower " + (i + 1),
                wheelAssemblies[i],
                typeof(Transform),
                true
            );

            if (GUILayout.Button("up", GUILayout.Width(32)) && i > 0)
            {
                Transform tmp = wheelAssemblies[i - 1];
                wheelAssemblies[i - 1] = wheelAssemblies[i];
                wheelAssemblies[i] = tmp;
            }

            if (GUILayout.Button("dn", GUILayout.Width(32)) && i < wheelAssemblies.Count - 1)
            {
                Transform tmp = wheelAssemblies[i + 1];
                wheelAssemblies[i + 1] = wheelAssemblies[i];
                wheelAssemblies[i] = tmp;
            }

            if (GUILayout.Button("x", GUILayout.Width(24)))
                removeIndex = i;

            EditorGUILayout.EndHorizontal();
        }

        if (removeIndex >= 0)
            wheelAssemblies.RemoveAt(removeIndex);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("add slot")) wheelAssemblies.Add(null);
        if (GUILayout.Button("clear")) wheelAssemblies.Clear();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("rollers", EditorStyles.boldLabel);
        includeInactiveChildren = EditorGUILayout.Toggle("include inactive children", includeInactiveChildren);
        sortRollers = EditorGUILayout.Toggle("sort rollers", sortRollers);
        using (new EditorGUI.DisabledScope(!sortRollers))
        {
            sortMode = (RollerSortMode)EditorGUILayout.EnumPopup("sort mode", sortMode);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("second cable", EditorStyles.boldLabel);
        buildSecondCable = EditorGUILayout.Toggle("build second cable", buildSecondCable);
        secondCableOffset = EditorGUILayout.Vector3Field("offset", secondCableOffset);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("sag", EditorStyles.boldLabel);
        enableSag = EditorGUILayout.Toggle("enable sag", enableSag);
        subdivisionsPerSpan = EditorGUILayout.IntSlider("subdivisions per span", subdivisionsPerSpan, 2, 64);
        sagMeters = EditorGUILayout.FloatField("sag meters (max drop)", sagMeters);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("chairs", EditorStyles.boldLabel);
        chairPrefab = (GameObject)EditorGUILayout.ObjectField("chair prefab", chairPrefab, typeof(GameObject), false);

        chairsParentA = (Transform)EditorGUILayout.ObjectField("chairs parent A (optional)", chairsParentA, typeof(Transform), true);
        chairsParentB = (Transform)EditorGUILayout.ObjectField("chairs parent B (optional)", chairsParentB, typeof(Transform), true);

        autoGenerateChairsAfterBuild = EditorGUILayout.Toggle("auto-generate after build", autoGenerateChairsAfterBuild);

        generateChairsOnA = EditorGUILayout.Toggle("generate chairs on cable A", generateChairsOnA);
        generateChairsOnB = EditorGUILayout.Toggle("generate chairs on cable B", generateChairsOnB);

        chairSpacing = EditorGUILayout.FloatField("chair spacing (m)", chairSpacing);
        chairStartOffsetA = EditorGUILayout.FloatField("start offset A (m)", chairStartOffsetA);
        chairStartOffsetB = EditorGUILayout.FloatField("start offset B (m)", chairStartOffsetB);

        chairLocalOffset = EditorGUILayout.Vector3Field("chair local offset", chairLocalOffset);

        keepChairsUpright = EditorGUILayout.Toggle("keep chairs upright", keepChairsUpright);
        chairPrefabFacesBackward = EditorGUILayout.Toggle("prefab faces backward", chairPrefabFacesBackward);
        cableBFacesBackward = EditorGUILayout.Toggle("cable B faces backward", cableBFacesBackward);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("build cable(s)"))
        {
            BuildCablesAndMaybeChairs();
        }
        if (GUILayout.Button("generate chairs only"))
        {
            GenerateChairsOnly();
        }
        if (GUILayout.Button("clear chairs"))
        {
            ClearChairs();
        }
        EditorGUILayout.EndHorizontal();
    }

    void BuildCablesAndMaybeChairs()
    {
        if (cableA == null)
        {
            Debug.LogError("cable A missing");
            return;
        }

        List<Vector3> basePoints = BuildPointListWithSag();
        if (basePoints.Count < 2)
        {
            Debug.LogError("not enough points");
            return;
        }

        // cable a
        ApplyPoints(cableA, basePoints);

        // cable b (offset)
        List<Vector3> pointsB = null;
        if (buildSecondCable)
        {
            pointsB = OffsetPoints(basePoints, secondCableOffset);

            if (cableB != null)
                ApplyPoints(cableB, pointsB);
        }

        Debug.Log("built cable(s). points: " + basePoints.Count);

        if (autoGenerateChairsAfterBuild)
        {
            GenerateChairsFromPaths(basePoints, pointsB);
        }
    }

    void GenerateChairsOnly()
    {
        List<Vector3> basePoints = BuildPointListWithSag();
        if (basePoints.Count < 2)
        {
            Debug.LogError("not enough points to place chairs");
            return;
        }

        List<Vector3> pointsB = null;
        if (buildSecondCable)
            pointsB = OffsetPoints(basePoints, secondCableOffset);

        GenerateChairsFromPaths(basePoints, pointsB);
    }

    void GenerateChairsFromPaths(List<Vector3> pointsA, List<Vector3> pointsB)
    {
        if (chairPrefab == null)
        {
            Debug.LogError("chair prefab missing");
            return;
        }
        if (chairSpacing <= 0.01f)
        {
            Debug.LogError("chair spacing must be > 0");
            return;
        }

        // cable a chairs
        if (generateChairsOnA && pointsA != null && pointsA.Count >= 2)
        {
            Transform parentA = EnsureParent(chairsParentA, "generated_chairs_A");
            ClearChairsUnder(parentA);
            GenerateChairsOnPath(pointsA, parentA, chairStartOffsetA, chairSpacing, false);
        }

        // cable b chairs
        if (generateChairsOnB && pointsB != null && pointsB.Count >= 2)
        {
            Transform parentB = EnsureParent(chairsParentB, "generated_chairs_B");
            ClearChairsUnder(parentB);

            // reverseFacing makes them look backward relative to travel direction
            bool reverseFacing = cableBFacesBackward;
            GenerateChairsOnPath(pointsB, parentB, chairStartOffsetB, chairSpacing, reverseFacing);
        }
    }

    void GenerateChairsOnPath(List<Vector3> path, Transform parent, float startOffset, float spacing, bool reverseFacing)
    {
        float totalLen = ComputePathLength(path);
        if (totalLen <= 0.01f)
        {
            Debug.LogError("chair path length is zero");
            return;
        }

        int count = 0;
        float d = startOffset;

        Vector3 lastForward = Vector3.forward;

        while (d < totalLen)
        {
            Vector3 pos;
            Vector3 tangent;
            SamplePath(path, d, out pos, out tangent);

            Vector3 forward = tangent;

            // reverse the chair orientation, used for cable b
            if (reverseFacing)
                forward = -forward;

            if (keepChairsUpright)
            {
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.0001f) forward = lastForward;
                forward.Normalize();
            }
            else
            {
                if (forward.sqrMagnitude < 0.0001f) forward = lastForward;
                forward.Normalize();
            }

            // if  prefabs front is backwards, flip it
            if (chairPrefabFacesBackward)
                forward = -forward;

            lastForward = forward;

            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

            Object obj = PrefabUtility.InstantiatePrefab(chairPrefab);
            GameObject chair = obj as GameObject;
            if (chair == null) break;

            Undo.RegisterCreatedObjectUndo(chair, "create chair");
            chair.transform.parent = parent;
            chair.transform.position = pos;
            chair.transform.rotation = rot;

            // drop it below cable
            chair.transform.position += rot * chairLocalOffset;

            count++;
            d += spacing;
        }

        Debug.Log("generated chairs under " + parent.name + ": " + count);
    }

    void ClearChairs()
    {
        Transform a = chairsParentA;
        Transform b = chairsParentB;

        if (a == null)
        {
            GameObject existingA = GameObject.Find("generated_chairs_A");
            if (existingA != null) a = existingA.transform;
        }
        if (b == null)
        {
            GameObject existingB = GameObject.Find("generated_chairs_B");
            if (existingB != null) b = existingB.transform;
        }

        if (a != null) ClearChairsUnder(a);
        if (b != null) ClearChairsUnder(b);
    }

    // cable building

    List<Vector3> BuildPointListWithSag()
    {
        List<List<Vector3>> perTower = new List<List<Vector3>>();

        for (int a = 0; a < wheelAssemblies.Count; a++)
        {
            Transform assembly = wheelAssemblies[a];
            if (assembly == null) continue;

            List<Transform> rollers = CollectRollers(assembly, includeInactiveChildren);
            if (rollers.Count == 0)
            {
                Debug.LogWarning("no rollers under: " + assembly.name);
                continue;
            }

            if (sortRollers)
                rollers.Sort((x, y) => CompareRollers(x, y, sortMode));

            List<Vector3> pts = new List<Vector3>();
            for (int i = 0; i < rollers.Count; i++)
                pts.Add(rollers[i].position);

            perTower.Add(pts);
        }

        if (perTower.Count < 2) return new List<Vector3>();

        List<Vector3> outPts = new List<Vector3>();

        for (int i = 0; i < perTower[0].Count; i++)
            outPts.Add(perTower[0][i]);

        for (int t = 0; t < perTower.Count - 1; t++)
        {
            List<Vector3> aPts = perTower[t];
            List<Vector3> bPts = perTower[t + 1];

            Vector3 start = aPts[aPts.Count - 1];
            Vector3 end = bPts[0];

            if (enableSag)
                InsertSagSpan(outPts, start, end, subdivisionsPerSpan, sagMeters);
            else
            {
                outPts.Add(Vector3.Lerp(start, end, 0.33f));
                outPts.Add(Vector3.Lerp(start, end, 0.66f));
            }

            for (int i = 0; i < bPts.Count; i++)
                outPts.Add(bPts[i]);
        }

        return outPts;
    }

    static void InsertSagSpan(List<Vector3> outPts, Vector3 start, Vector3 end, int subdiv, float sagDropMeters)
    {
        for (int i = 1; i <= subdiv; i++)
        {
            float t = (float)i / (subdiv + 1);
            Vector3 p = Vector3.Lerp(start, end, t);

            float curve = 4f * t * (1f - t);
            p.y -= curve * sagDropMeters;

            outPts.Add(p);
        }
    }

    static List<Vector3> OffsetPoints(List<Vector3> pts, Vector3 offset)
    {
        List<Vector3> outPts = new List<Vector3>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
            outPts.Add(pts[i] + offset);
        return outPts;
    }

    static void ApplyPoints(LineRenderer lr, List<Vector3> points)
    {
        Undo.RecordObject(lr, "build chairlift cable");
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
        EditorUtility.SetDirty(lr);
    }

    //  roller collection

    static List<Transform> CollectRollers(Transform root, bool includeInactive)
    {
        List<Transform> rollers = new List<Transform>();
        MeshRenderer[] meshes = root.GetComponentsInChildren<MeshRenderer>(includeInactive);

        for (int i = 0; i < meshes.Length; i++)
        {
            Transform t = meshes[i].transform;
            if (!rollers.Contains(t))
                rollers.Add(t);
        }

        return rollers;
    }

    static int CompareRollers(Transform a, Transform b, RollerSortMode mode)
    {
        float av = 0f;
        float bv = 0f;

        switch (mode)
        {
            case RollerSortMode.LocalX: av = a.localPosition.x; bv = b.localPosition.x; break;
            case RollerSortMode.LocalY: av = a.localPosition.y; bv = b.localPosition.y; break;
            case RollerSortMode.LocalZ: av = a.localPosition.z; bv = b.localPosition.z; break;
        }

        if (av < bv) return -1;
        if (av > bv) return 1;
        return 0;
    }

    // path sampling

    static float ComputePathLength(List<Vector3> pts)
    {
        float len = 0f;
        for (int i = 0; i < pts.Count - 1; i++)
            len += Vector3.Distance(pts[i], pts[i + 1]);
        return len;
    }

    static void SamplePath(List<Vector3> pts, float distance, out Vector3 pos, out Vector3 tangent)
    {
        pos = pts[0];
        tangent = (pts.Count > 1) ? (pts[1] - pts[0]) : Vector3.forward;

        float d = distance;

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 a = pts[i];
            Vector3 b = pts[i + 1];
            float seg = Vector3.Distance(a, b);

            if (seg <= 0.00001f) continue;

            if (d <= seg)
            {
                float t = d / seg;
                pos = Vector3.Lerp(a, b, t);
                tangent = (b - a);
                return;
            }
            d -= seg;
        }

        pos = pts[pts.Count - 1];
        tangent = pts[pts.Count - 1] - pts[pts.Count - 2];
    }

    // chair parenting

    static Transform EnsureParent(Transform provided, string name)
    {
        if (provided != null) return provided;

        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing.transform;

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "create chairs parent");
        return go.transform;
    }

    static void ClearChairsUnder(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform c = parent.GetChild(i);
            if (c != null)
                Undo.DestroyObjectImmediate(c.gameObject);
        }
    }
}
