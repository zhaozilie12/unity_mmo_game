local cls, super = defClass("Game")


cls.require = function(var, ...)
    local env = Env.new(var)
    
    local cls = nil
    for i,v in ipairs({"game/Game", "game/GameNew", "game/level0/GameL0" , "game/level1/GameL1" , "game/level2/GameL2", "game/level3/GameL3" , ...}) do
        cls = Env.require(v, env)
    end
    return cls
end



