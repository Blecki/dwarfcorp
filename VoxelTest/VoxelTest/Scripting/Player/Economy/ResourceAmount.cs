namespace DwarfCorp
{

    /// <summary>
    /// This is just a struct of two things: a resource, and a number of that resource.
    /// This is used instead of a list, since there is nothing distinguishing resources from each other.
    /// </summary>
    public class ResourceAmount
    {
        public Resource ResourceType { get; set; }
        public float NumResources { get; set; }
    }

}