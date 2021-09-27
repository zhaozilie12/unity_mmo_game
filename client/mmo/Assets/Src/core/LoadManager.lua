--加载

local cls, super = defClassStatic("LoadManager")

LoadManager.Scene = {
    WorldMap = 1
}

LoadManager.SceneNameList = {
    "WorldMap"
}

LoadManager.loadFromResource = function(res_mame)
    return Resources.Load(res_mame)
end

LoadManager.loadScene = function(scene)
    local scene_id = LoadManager.Scene[scene]
    local sceneName = LoadManager.SceneNameList[scene_id]
    SceneManagement.SceneManager.LoadSceneAsync(sceneName, SceneManagement.LoadSceneMode.Single);
end