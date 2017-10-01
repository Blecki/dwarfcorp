using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;

namespace DwarfCorp.Saving
{
    /// <summary>
    /// A saveable chunk of data. Must be a POD type and must be serializable.
    /// </summary>
    public class Nugget
    {
        public System.Type AssociatedType;
        public int Version;

        [JsonIgnore]
        public Object LoadedObject;

        public static int FindSaveableObjectVersion(ISaveableObject SaveableObject)
        {
            var attributes = SaveableObject.GetType().GetCustomAttributes(false);
            foreach (var attribute in attributes.OfType<SaveableObjectAttribute>())
                return attribute.Version;
            return 0;
        }
    }

    public class GenericNugget : Nugget
    {
        public Object Data;
    }

    public interface ISaveableObject
    {
        Nugget SaveToNugget(Saver SaveSystem);
        void LoadFromNugget(Loader SaveSystem, Nugget From);
    }

    public class SaveableObjectAttribute : Attribute
    {
        public int Version = 0;

        public SaveableObjectAttribute(int Version)
        {
            this.Version = Version;
        }
    }

    public class NuggetUpgrader
    {
        public System.Type AssociatedType;
        public int SourceVersion;
        public int DestinationVersion;
        public System.Reflection.MethodInfo Method;
    }

    public class NuggetUpgraderAttribute : Attribute
    {
        public System.Type AssociatedType;
        public int SourceVersion;
        public int DestinationVersion;

        public NuggetUpgraderAttribute(System.Type AssociatedType, int SourceVersion, int DestinationVersion)
        {
            this.AssociatedType = AssociatedType;
            this.SourceVersion = SourceVersion;
            this.DestinationVersion = DestinationVersion;
        }
    }

    public class Saver
    {
        private Dictionary<Object, Nugget> CreatedNuggets = new Dictionary<object, Nugget>();
        private List<String> GenericSaveableTypes = new List<string>();

        public Nugget SaveObject(Object SaveableObject)
        {
            if (CreatedNuggets.ContainsKey(SaveableObject))
                return CreatedNuggets[SaveableObject];

            var asSaveable = SaveableObject as ISaveableObject;
            Nugget result = null;

            if (asSaveable != null)
            {
                result = asSaveable.SaveToNugget(this);
                result.AssociatedType = SaveableObject.GetType();
                result.Version = Nugget.FindSaveableObjectVersion(asSaveable);
            }
            else
            {
                var generic = new GenericNugget();
                result = generic;
                generic.Data = SaveableObject;
                result.AssociatedType = SaveableObject.GetType();
                result.Version = -1;

                GenericSaveableTypes.Add(SaveableObject.GetType().FullName);
            }

            CreatedNuggets.Add(SaveableObject, result);
            return result;
        }

        public List<String> GetGenericallySavedTypes()
        {
            return GenericSaveableTypes.Distinct().ToList();
        }
    }

    public class Loader
    {
        private List<NuggetUpgrader> Upgraders;

        private void DiscoverUpgraders()
        {
            if (Upgraders != null) return;

            Upgraders = new List<NuggetUpgrader>();

            foreach (var @class in System.Reflection.Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (var method in @class.GetMethods())
                {
                    if (method.IsStatic && method.ReturnType == typeof(Nugget))
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length != 1) continue;
                        if (parameters[0].ParameterType != typeof(Nugget)) continue;

                        var attributes = method.GetCustomAttributes(false);

                        foreach (var attribute in attributes)
                        {
                            var upgraderAttribute = attribute as NuggetUpgraderAttribute;
                            if (upgraderAttribute != null)
                            {
                                Upgraders.Add(new NuggetUpgrader
                                {
                                    AssociatedType = upgraderAttribute.AssociatedType,
                                    SourceVersion = upgraderAttribute.SourceVersion,
                                    DestinationVersion = upgraderAttribute.DestinationVersion,
                                    Method = method
                                });

                                break;
                            }
                        }
                    }
                }
            }
        }

        public Object LoadObject(Nugget From)
        {
            if (From.LoadedObject != null) return From.LoadedObject;

            if (From is GenericNugget)
            {
                var generic = From as GenericNugget;
                generic.LoadedObject = generic.Data;
                return generic.Data;
            }

            var result = Activator.CreateInstance(From.AssociatedType) as ISaveableObject;
            if (result == null)
                throw new InvalidOperationException("Attempted to load object that is not saveable.");

            var resultVersion = Nugget.FindSaveableObjectVersion(result);
            var currentNugget = From;

            while (resultVersion > currentNugget.Version)
            {
                DiscoverUpgraders();
                var upgrader = Upgraders.FirstOrDefault(u =>
                    u.AssociatedType == currentNugget.AssociatedType
                    && u.SourceVersion == currentNugget.Version);
                if (upgrader == null)
                    throw new InvalidOperationException("Failed to upgrade nugget.");

                currentNugget = upgrader.Method.Invoke(null, new Object[] { currentNugget }) as Nugget;
                currentNugget.AssociatedType = upgrader.AssociatedType;
                currentNugget.Version = upgrader.DestinationVersion;
            }

            result.LoadFromNugget(this, currentNugget);
            From.LoadedObject = result;
            return result;
        }
    }

    // Usage Example!

    public class SampleNugget : Nugget
    {
        public float Data;
    }

    [SaveableObject(0)]
    public class SampleSaveable : ISaveableObject
    {
        public float Data;

        public void LoadFromNugget(Loader SaveSystem, Nugget From)
        {
            Data = (From as SampleNugget).Data;
        }

        public Nugget SaveToNugget(Saver SaveSystem)
        {
            return new SampleNugget { Data = Data };
        }
    }

    // Oh no - we need to upgrade this class but we already have thousands of NuggetSamples saved!

    public class SampleNugget2 : Nugget
    {
        public double Data;

        [NuggetUpgrader(typeof(SampleSaveable), 0, 1)]
        Nugget __upgrade(Nugget From)
        {
            return new SampleNugget2 { Data = (From as SampleNugget).Data };
        }
    }

    [SaveableObject(1)]
    public class _SampleSaveable : ISaveableObject // Class name would actually be the same.
    {
        public double Data;

        public void LoadFromNugget(Loader SaveSystem, Nugget From)
        {
            Data = (From as SampleNugget2).Data;
        }

        public Nugget SaveToNugget(Saver SaveSystem)
        {
            return new SampleNugget2 { Data = Data };
        }
    }
}
