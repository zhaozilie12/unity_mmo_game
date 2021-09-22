using System;
using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using XLua;

using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;


public class LuaGlobal : MonoBehaviour {
    static LuaGlobal     bhv = null;
	static public LuaEnv env = null;

    public string _EntryApp = "assets/streamingpacked/src/boot/main.bytes";
    public string _EntryDev = "Src/boot/main.lua";
    public string _EntryArt = "Extern/Build/boot/main4art.lua";

    public bool useLocal = true;

	void Start () {
		if (env == null){
			Setup();
			string src;
			if (useLocal) 
            {
                src = Application.dataPath + "/" + _EntryDev;
				env.DoString("dofile(\""+src+"\")");
			}
            else 
            {
				TextAsset ver = (TextAsset)Resources.Load("version");
				LuaTable ret = (LuaTable)env.DoString(ver.bytes)[0];
				string abfile = ret.Get<string>("bin");

				if(Application.platform == RuntimePlatform.Android)
					src = Application.dataPath + "!assets/" + abfile;
				else
					src = Application.streamingAssetsPath + "/" + abfile;

				AssetBundle ab = AssetBundle.LoadFromFile(src);
				if(ab){
                    TextAsset asset = (TextAsset)ab.LoadAsset(_EntryApp);
					ab.Unload(false);
					if(asset)
						env.DoString(asset.bytes, "boot/main");
                    else
                        Debug.LogError("LuaGlobal: load boot/main failed");

				}
			}
		}
	}

    void Setup()
    {
        gameObject.name = "LuaGlobal";
        DontDestroyOnLoad(gameObject);
        bhv = this;
        env = new LuaEnv();

        env.Global.Set("_IsType", new LuaCSFunction(IsType));
        env.Global.Set("_TickGC", new LuaCSFunction(TickGC));
        env.Global.Set("_Yieldk", new LuaCSFunction(Yieldk));
        env.Global.Set("BUILD_ENV", LuaHelper.GetBuildEnv());
    }

    void LateUpdate()
    {
        runActionQueue();
        if (env != null)
            env.Tick();
    }

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    public static int IsType(RealStatePtr L)
    {
        ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);

        object obj = translator.SafeGetCSObj(L, 1);
        Type type;
        translator.Get(L, 2, out type);

        if (obj != null && type != null && type.IsInstanceOfType(obj))
        {
            LuaAPI.lua_pushboolean(L, true);
        }
        else
        {
            LuaAPI.lua_pushboolean(L, false);
        }
        return 1;
    }

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    public static int TickGC(RealStatePtr L)
    {
        env.Tick();
        return 0;
    }

    [MonoPInvokeCallback(typeof(LuaCSFunction))]
    public static int Yieldk(RealStatePtr L)
    {
        ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
        try
        {
            object y = translator.GetObject(L, 1);
            LuaFunction f;
            translator.Get(L, 2, out f);
            bhv.StartCoroutine(yieldReturn(y, f));
        }catch(System.Exception e){
            return LuaAPI.luaL_error(L, "[c# exception]" + e);
        }
        return 0;
    }

    public static IEnumerator yieldReturn(object y, LuaFunction f)
    {
        if (y is IEnumerator)
            yield return bhv.StartCoroutine((IEnumerator)y);
        else
            yield return y;
    }


    private Hashtable cacheHash = Hashtable.Synchronized(new Hashtable());
    public static void CacheObject(object obj, int cnt)
    {
        if (bhv)
            bhv.setObjectCache(obj, cnt);
    }

    public void setObjectCache(object obj, int cnt)
    {
        if (cacheHash.ContainsKey(obj))
            cnt = (int)cacheHash[obj] + cnt;

        if (cnt <= 0)
            cacheHash.Remove(obj);
        else
            cacheHash[obj] = cnt;
    }
   


    private List<Action> actionWait = new List<Action>();
    private List<Action> actionPlay = new List<Action>();
    public static void QueueAction(Action f)
    {
        if (bhv)
            bhv.addActionQueue(f);
    }

    public void addActionQueue(Action f)
    {
        lock (actionWait)
        {
            actionWait.Add(f);
        }
    }

    public void runActionQueue()
    {
        if (actionWait.Count > 0)
        {
            lock (actionWait)
            {
                actionPlay.AddRange(actionWait);
                actionWait.Clear();
            }

            int cnt = actionPlay.Count;
            int act = 0;
            try
            {
                for (int i = 0; i < cnt; i++)
                {
                    act = act + 1;
                    actionPlay[i]();
                }
            }
            finally
            {
                if (act == cnt)
                    actionPlay.Clear();
                else
                    actionPlay.RemoveRange(0, act);
            }
        }
    }
    
}


