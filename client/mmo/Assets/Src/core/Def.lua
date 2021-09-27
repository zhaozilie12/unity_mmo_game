--@func defClass(name, super=nil)
--@desc 定义类
--@ret  class:定义的类
--@ret  super:定义类的父类，同输入super
--@arg  name:string，类名称
--@arg  super:table，已经定义的其他类

--@func defClassStatic(name, super=nil)
--@desc 定义静态类，生成的类不含new方法，直接通过类名调用
--@ret  class:定义的类
--@ret  super:定义类的父类，同输入super
--@arg  name:string，类名称
--@arg  super:table，已经定义的其他类
------------------------------------

local type = type
local rawget = rawget
local rawset = rawset
local getmetatable = getmetatable
local setmetatable = setmetatable

local _cls_null_inst = {}
local _cls_null_data = {
    __cls_type = _cls_null_inst,
	__cls_name = "[cls.null]",
    __cls_exit = true,
    exit = function()
        Log.i("[cls.null]exit")
    end,
    valid = function() 
        return false 
    end,
}

setmetatable(_cls_null_inst, {
    __call = function()
        Log.e("[cls.null]call")
    end,
    __index = function(t, k)
        if _cls_null_data[k] then
            return _cls_null_data[k]
        end
        Log.e("[cls.null]index", k)
        return nil
    end,
    __newindex = function(t, k, v)
        Log.e("[cls.null]newindex", k, v)
    end,
    __tostring = function()
        Log.e("[cls.null]tostring")
        return "cls.null" 
    end
})

local _cls_base = {
    clear = function(self)

    end,
    
    valid = function(self)
        return (not self.__cls_exit)
    end,
    
    exit = function(self)
        if self.__cls_exit then
            return
        end
        self.__cls_exit = true
        
        self:clear()
        if not self.__cls_call_clear then
            Log.e("[cls.exit]missing super.clear", self.__cls_name)
        end
        
        self:onExit()
        if not self.__cls_call_onExit then
            Log.e("[cls.exit]missing super.onExit", self.__cls_name)
        end
        
        for k,v in pairs(self) do
            if type(v) == "table" then
                rawset(self, k, _cls_null_inst)
            end
        end

        if self == self.__cls_type.__last_inst then
            self.__cls_type.__last_inst = nil
        end
        
        self.__cls_exit = true
    end,
    
    onExit = function(self)
        if not self.__cls_inst then
            Log.e("[cls.onExit]invalid self", self.__cls_name)
        end
        self.__cls_call_onExit = true
        
        for k,v in pairs(self) do
            if type(v) == "table" then
                if v.__cls_inst then
                    if not v.__cls_exit then
                        v:exit()
                    end
                else
                    for k,t in pairs(v) do
                        if type(t) == "table" and t.__cls_inst then
                            if not t.__cls_exit then
                                t:exit()
                            end
                        end
                    end
                end
            end
        end
    end,
}

local _cls_mark = function(name, cls, env)
    local p = env or _ENV
    for ns in string.gmatch(name, "(.-)%.") do
        local t = rawget(p, ns)
        if not t then
            t = {}
            rawset(p, ns, t)
        end
        p = t
    end
    
    local mod = string.match(name, "([^%.]+)$")

    rawset(p, mod, cls)
end


function defClass(name, super, env)
    env = env or _ENV
	if rawget(env, name) then
		Log.e("defClass:redefined", name)
	end
	
	local cls = {}
	cls.__cls_type = cls
	cls.__cls_name = name
    cls["__is"..name] = true

    super = super or _cls_base
    setmetatable(cls, {__index = super})

    local cls_meta = {__index = cls}

	cls.new = function(...)
		local obj = {__cls_inst = true}
        setmetatable(obj, cls_meta)
        cls.__last_inst = obj
        if obj.ctor then
            obj:ctor(...)
        end
		return obj
	end
	
    _cls_mark(name, cls, env)
	return cls, super
end


function defClassStatic(name, super, env)
    env = env or _ENV
	if rawget(env, name) then
		Log.e("defClassStatic:redefined", name)
	end

	local cls = {}
	cls.__cls_type = cls
	cls.__cls_name = name
	cls.__cls_static = true

    if super then
        setmetatable(cls, {__index = super})
    end
    
    _cls_mark(name, cls, env)
	return cls, super
end


local _GG = _G
local _CS = _GG.CS
local _UE = _CS.UnityEngine
local _UI = _UE.UI

local namespaces={
    UnityEngine.AI,
    UnityEngine.Animations,
    UnityEngine.Audio,
    UnityEngine.Events,
    UnityEngine.EventSystems,
    UnityEngine.Playables,
    UnityEngine.Rendering,
    UnityEngine.SceneManagement,
    UnityEngine.Sprites,
    UnityEngine.TextCore,
    UnityEngine.Tilemaps,
    UnityEngine.Timeline,
    UnityEngine.U2D,
    UnityEngine.Video,
}

local _global_find = function(k)
    local v = _GG[k]
    if v then
        return v
    end
    
    v = rawget(_CS, k)
    if v then
        return v
    end

    xlua.import_type(k)
    v = rawget(_CS, k)
    if v then
        return v
    end
    
    v = rawget(_UI, k)
    if v then
        return v
    end
    
    xlua.import_type("UnityEngine.UI."..k)
    v = rawget(_UI, k)
    if v then
        return v
    end

    v = rawget(_UE, k)
    if v then
        return v
    end
    
    xlua.import_type("UnityEngine."..k)
    v = rawget(_UE, k)

    return v
end

local _global_meta = {
    __newindex = function(t, k, v)
        Log.e("setGlobal:failed", t, k, v, debug.traceback())
    end,
    __index = function(t, k)
        local v = _global_find(k)
        if v then
            rawset(t, k, v)
            return v
        else
            return nil
        end
    end
}
setmetatable(_ENV, _global_meta)







