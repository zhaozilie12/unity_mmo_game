_G.GAME_MAIN_FILE = "main/main"

print("[Launcher]boot for Dev")

local ReadFile = CS.LuaHelper.ReadFile
local dataPath = CS.UnityEngine.Application.dataPath

local _require = function(filename, env, reshash)
    env = env or _G
    local filepath = dataPath.."/src/"..filename..".lua"

    local src = ReadFile(filepath)
    if src == nil or src == "" then
        error("Load Text->"..filename..":"..tostring(src))
    end

    local f,err = load(src, filename..".lua", "bt", env)
    if f then
        return f()
    else
        error(tostring(err).."\n"..debug.traceback())
    end
end

local _newenv = function(env)
    local _G = _G
    local _E = env or _G
    local rawset = _G.rawset
    return _G.setmetatable(
    {
        _G = _G,
        ENV_REQUIRE = _require,
    }, 
    {
        __index = function(t, k)
            local v = _E[k]
            rawset(t, k, v)
            return v
        end,
    })
end

local _ENV = _newenv()
_ENV.ENV_PACKAGE_DIR = CS.UnityEngine.Application.streamingAssetsPath .. "/"
_ENV.require = function(filename, _env, _reshash)
    _env = _env or _ENV
    return _require(filename, _env, _reshash or _env.ENV_RESHASH or _ENV.ENV_RESHASH)
end

require(_G.GAME_MAIN_FILE)






