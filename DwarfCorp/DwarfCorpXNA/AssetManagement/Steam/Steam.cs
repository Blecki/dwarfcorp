using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Facepunch.Steamworks;

namespace DwarfCorp.AssetManagement.Steam
{
    public class Steam : IDisposable
    {
        public Client Client { get; set; }
        public uint AppID { get; }
        // TODO: real app id
        public const uint DefaultAppID = 252390;
        public Steam(uint appID = DefaultAppID)
        {
            AppID = appID;
            //Facepunch.Steamworks.Config.ForcePlatform(Facepunch.Steamworks.OperatingSystem.Windows, Architecture.x86);
            Client = new Client(AppID);
        }

        public IEnumerable<Workshop.Item> QueryWorkshop()
        {
            if (Client == null || !Client.IsValid)
            {
                yield break;
            }

            var query = Client.Workshop.CreateQuery();
            query.UserId = Client.SteamId;
            query.UserQueryType = Workshop.UserQueryType.Published;
            query.Run();
            query.Block();
            foreach (var item in query.Items)
            {
                yield return item;
            }
        }

        public void CreateMod(string filepath)
        {
            if (Client == null || !Client.IsValid)
            {
                throw new InvalidOperationException("Steam is not initialized. Are you logged in?");
            }

            ModMetaData metadata = null;
            try
            {
                metadata = FileUtils.LoadJsonFromAbsolutePath<ModMetaData>(filepath + Program.DirChar + "meta.json");
            }
            catch (Exception exception)
            {
                throw exception;
            }

            var item = Client.Workshop.CreateItem(AppID);
            item.Folder = filepath;
            item.Description = metadata.Description;
            item.Title = metadata.Name;
            item.Tags = metadata.Tags;
            item.PreviewImage = filepath + Program.DirChar + metadata.PreviewURL;
            item.ChangeNote = metadata.ChangeNote;
            item.Visibility = Workshop.Editor.VisibilityType.Public;
            item.WorkshopUploadAppId = AppID;
          
            item.Publish();
            System.Threading.Thread.Sleep(1000);
            while (item.Publishing)
            {
                System.Threading.Thread.Sleep(1000);
                try
                {
                    Client.Update();
                }
                catch (Exception exception)
                {
                    item.Delete();
                    throw exception;
                }
                System.Threading.Thread.Sleep(10);
                Console.WriteLine("Progress: " + item.Progress);
                Console.WriteLine("BytesUploaded: " + item.BytesUploaded);
                Console.WriteLine("BytesTotal: " + item.BytesTotal);
            }
            Console.WriteLine("item.Id: {0}", item.Id);
            if (item.Id == 0)// || item.Error != null)
            {
                item.Delete();
                throw new Exception(string.Format("Failed to upload mod: {0}", item.Error));
            }
        }

        public void Dispose()
        {
            if (Client != null)
            {
                Client.Dispose();
            }
        }
    }
}
