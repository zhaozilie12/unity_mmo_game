using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

[XLua.LuaCallCSharp]
public class LuaEventUpdate : MonoBehaviour
{
    private LuaFunction m_LuaFunc;

    private void Update()
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
