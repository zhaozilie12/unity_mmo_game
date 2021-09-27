UnityEngine     = CS.UnityEngine
GameObject      = UnityEngine.GameObject
Quaternion      = UnityEngine.Quaternion
Events          = UnityEngine.Events
EventSystems    = UnityEngine.EventSystems
SceneManagement = UnityEngine.SceneManagement

local type = type
local rawget = rawget
local rawset = rawset
local getmetatable = getmetatable
local setmetatable = setmetatable


function HackFunc(cls, funcname)
    local f = rawget(cls, funcname .. "_origin_")
    if not f then
        f = cls[funcname]
        rawset(cls, funcname .. "_origin_", f)
    end
    return f
end

local GameObjectFind = HackFunc(GameObject, "Find")
GameObject.Find = function(name, includeInactive)
    local go = GameObjectFind(name)
    if go == nil and includeInactive then
        local root, rest = string.match(name or "", "^(/.-)(/.+)$")
        if root and rest then
            go = GameObjectFind(root)
            for v in string.gmatch(rest.."/", "/(.-)/") do
                if go then
                    go = go[v]
                else
                    break
                end
            end
        end
    end
    return go
end


local GameObjectCls = xlua.metatable_operation(typeof(GameObject))

local GameObjectClsIndex = HackFunc(GameObjectCls, "__index")
GameObjectCls.__index = function(ud, k)
    local v = rawget(GameObjectCls, k)
    if v then
        return v
    end
    
    v = GameObjectClsIndex(ud, k)
    if v then
        return v
    end

    
    local ktype = type(k)
    if ktype == "string" then--Find Child Object
        local child = ud.transform:Find(k)
        if child then
            return child.gameObject
        end
    elseif ktype == "table" then--Find Component
        local comp = ud:GetComponent(typeof(k))
        if comp then
            return comp
        end
    else
        error("GameObject:__index failed  "..tostring(k))
    end

    return nil
end




