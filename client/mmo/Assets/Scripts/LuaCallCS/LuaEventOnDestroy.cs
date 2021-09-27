using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

[XLua.LuaCallCSharp]
public class LuaEventOnDestroy : MonoBehaviour
{
    private LuaFunction m_LuaFunc;
    private void OnDestroy()
    {
        if (m_LuaFunc != null)
        {
            m_LuaFunc.Action();
        }
    }

    public void Bind(LuaFunction luaFunc)
    {
        m_LuaFunc = luaFunc;
    }
}
