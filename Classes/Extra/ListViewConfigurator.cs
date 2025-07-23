using BrightIdeasSoftware;
using Sweep.Models;

namespace Sweep.UI
{
    public static class ListViewConfigurator
    {
        public static void Configure(ObjectListView listView, int port)
        {
            listView.AllColumns.Clear();

            listView.AllColumns.Add(new OLVColumn("Screen", nameof(ClientInfo.Screen))
            {
                Width = 60,
                ImageAspectName = nameof(ClientInfo.Screen),
                Renderer = new ImageRenderer()
            });
            listView.AllColumns.Add(new OLVColumn("IP", nameof(ClientInfo.IP)) { Width = 100 });
            listView.AllColumns.Add(new OLVColumn("Country", nameof(ClientInfo.Country)) { Width = 90 });
            listView.AllColumns.Add(new OLVColumn("Flag", nameof(ClientInfo.Flag))
            {
                Width = 40,
                ImageAspectName = nameof(ClientInfo.Flag),
                Renderer = new ImageRenderer()
            });
            listView.AllColumns.Add(new OLVColumn("ID", nameof(ClientInfo.ID)) { Width = 100 });
            listView.AllColumns.Add(new OLVColumn("Username", nameof(ClientInfo.Username)) { Width = 80 });
            listView.AllColumns.Add(new OLVColumn("Operating System", nameof(ClientInfo.OperatingSystem)) { Width = 113 });
            listView.AllColumns.Add(new OLVColumn("CPU", nameof(ClientInfo.CPU)) { Width = 136 });
            listView.AllColumns.Add(new OLVColumn("GPU", nameof(ClientInfo.GPU)) { Width = 152 });
            listView.AllColumns.Add(new OLVColumn("UAC?", nameof(ClientInfo.UAC)) { Width = 60 });
            listView.AllColumns.Add(new OLVColumn("HWID", nameof(ClientInfo.HWID)) { Width = 157 });

            listView.RebuildColumns();
            listView.FullRowSelect = true;
            listView.OwnerDraw = true;
            listView.ShowImagesOnSubItems = true;
            listView.View = System.Windows.Forms.View.Details;

            listView.DrawItem += (s, e) => e.DrawDefault = true;
            listView.DrawColumnHeader += (s, e) =>
            {
                e.DrawBackground();
                e.DrawDefault = true;
            };
        }
    }
}
