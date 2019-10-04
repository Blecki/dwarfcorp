namespace DwarfCorp
{
    public enum DesignationType
    {
        _None   = 0,

        Dig     = 1,
        Gather  = 2,
        Attack  = 4,
        Wrangle = 8,
        Chop    = 16,
        Put     = 32,
        Plant   = 64, 
        PlaceObject   = 128,

        _All     = Dig | Gather | Attack | Wrangle | Chop | Put | Plant | PlaceObject,
    }
}
