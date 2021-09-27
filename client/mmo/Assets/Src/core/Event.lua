--@func Event.add(go, ev, fn, obj=nil)
--@desc 给go绑定一个事件回调,同GameObject.BindEvent
--@arg  ev:int/string,事件名称或事件id（两种都支持），见Event
--@arg  fn:function,回调函数，参数见unity定义
--@arg  obj:table，如果obj~=nil，调用时作为self传给fn， fn(obj, ...)

--@func Event.del(go, ev=nil, fn=nil, obj=nil)
--@desc 删除给go绑定的事件回调，条件判断 （ev && fn && obj），同GameObject.UnbindEvent
--@arg  ev:int/string,事件名称或事件id（两种都支持），见Event，ev==nil则表示所有事件ev==*
--@arg  fn:function,回调函数，参数见unity定义,fn==nil则表示所有函数fn==*
--@arg  obj:table，obj==nil则表示所有参数obj==*
------------------------------------
--Event = nil--ignore UnityEngine.Event

local Event, super = defClassStatic("Event")

Event.Awake = 1
Event.Update = 2
Event.OnDestroy = 3

Event.evlist = {
    "Awake",
    "Update",
    "OnDestroy"
}

Event.cslist = {
    LuaEventAwake,
    LuaEventUpdate,
    LuaEventOnDestroy
}

Event.golist = {}
Event.gocomp = {}

Event._id = function(ev)
    if Event.cslist[ev] then
        return ev
    end
    
    if Event[ev] then
        return Event[ev]
    end
    
    Log.e("[Event]invalid", ev)
end

Event.add = function(go, ev, fn, obj)
    ev = Event._id(ev)
    local list = Event.golist[go]
    local comp = Event.gocomp[go]
    if list == nil or comp == nil then
        list = {}
        Event.golist[go] = list
        comp = {}
        Event.gocomp[go] = comp
        
        Event.add(go, Event.OnDestroy, function(flag)
            Event.golist[go] = nil   
            Event.gocomp[go] = nil
        end)
    end
    
    local evlist = list[ev]
    if not evlist then
        evlist = {}
        list[ev] = evlist
        comp[ev] = go:AddComponent(typeof(Event.cslist[ev]))
        comp[ev]:Bind(function(...)
            local copy = {}
            for i,v in ipairs(evlist) do
                copy[i] = v
            end
            for i,v in ipairs(copy) do
                if v[3] then
                    if v[2] then
                        if type(v[2]) ~= "table" or v[2].__cls_exit ~= true then
                            v[1](v[2], ...)
                        end
                    else
                        v[1](...)
                    end
                end
            end
        end)
    end
    if fn then
        table.insert(evlist, {fn, obj, true})
    end
end

Event.del = function(go, ev, fn, obj)
    if ev then
        ev = Event._id(ev)
    end
    local list = Event.golist[go]
    if list then
        for k,evlist in pairs(list) do
            if ev == nil or ev == k then
                for i = #evlist, 1, -1 do
                    local v = evlist[i]
                    if (fn == nil or fn == v[1]) and (obj == nil or obj == v[2]) then
                        v[3] = false
                        table.remove(evlist, i)
                    end
                end
            end
        end
    end
end

--protect Event
setmetatable(Event, {
    __newindex = function(t, k, v)
		Log.e("Event modify limited", t, k, v)
	end,
    __index = function(t, k)
        Log.e("Event define missing", t, k)
    end
})



