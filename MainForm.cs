using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace VRSEX
{
    public partial class MainForm : Form
    {
        public static MainForm Instance { get; private set; } = null;
        public static bool LastLoginSuccess { get; private set; } = true;

        // SharpDX
        public float RotationX { get { return (float)((double)nud_RX.Value * (Math.PI / 180)); } }
        public float RotationY { get { return (float)((double)nud_RY.Value * (Math.PI / 180)); } }
        public float RotationZ { get { return (float)((double)nud_RZ.Value * (Math.PI / 180)); } }
        public float X { get { return (float)((double)nud_TX.Value / 100); } }
        public float Y { get { return (float)((double)nud_TY.Value / 100); } }
        public float Z { get { return (float)((double)nud_TZ.Value / 100); } }
        public float ScaleXYZ { get { return (float)((double)nud_S3.Value / 100); } }

        // MainForm
        private bool m_Shutdown = false;

        // VRCLog
        private DateTime m_NextUpdateLog = DateTime.MaxValue;

        // VRCEX
        private ApiUser m_CurrentUser = null;
        private DateTime m_NextFetchVisits = DateTime.MaxValue;
        private DateTime m_NextFetchCurrentUser = DateTime.MaxValue;
        private DateTime m_NextFetchModeration = DateTime.MaxValue;
        private DateTime m_NextFetchFavorite = DateTime.MaxValue;
        private Dictionary<string, ApiPlayerModeration> m_Moderation = null;
        private Dictionary<string, ApiUser> m_Friends = new Dictionary<string, ApiUser>();
        private Dictionary<string, int> m_FavoriteFriends = new Dictionary<string, int>();
        private Dictionary<string, string> m_WorldNames = new Dictionary<string, string>();
        private HashSet<string> m_GPS = new HashSet<string>();

        // WINAPI
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public MainForm()
        {
            Instance = this;
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            label_author.Text = VRSEX.APP + " by pypy (mina#5656)";
            checkbox_discord_presence.Checked = "true".Equals(LocalConfig.GetString("DiscordPresence", "true"), StringComparison.OrdinalIgnoreCase);
            checkbox_show_location.Checked = "true".Equals(LocalConfig.GetString("ShowLocation", "true"), StringComparison.OrdinalIgnoreCase);
            checkbox_overlay.Checked = "true".Equals(LocalConfig.GetString("Overlay", "true"), StringComparison.OrdinalIgnoreCase);
            OnLogin();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !m_Shutdown;
        }

        private void button_logout_Click(object sender, EventArgs e)
        {
            LastLoginSuccess = false;
            VRCApi.ClearCookie();
            OnLogin();
        }

        private void checkbox_discord_CheckedChanged(object sender, EventArgs e)
        {
            LocalConfig.SetString("DiscordPresence", checkbox_discord_presence.Checked.ToString());
            LocalConfig.SetString("ShowLocation", checkbox_show_location.Checked.ToString());
        }


        private void checkbox_overlay_CheckedChanged(object sender, EventArgs e)
        {
            LocalConfig.SetString("Overlay", checkbox_overlay.Checked.ToString());
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (m_CurrentUser != null)
            {
                /*if (DateTime.Now.CompareTo(m_NextUpdateVisits) >= 0)
                {
                    VRCApi.FetchVisits();
                    m_NextUpdateVisits = DateTime.Now.AddSeconds(60);
                }*/
                if (DateTime.Now.CompareTo(m_NextFetchCurrentUser) >= 0)
                {
                    ApiUser.FetchCurrentUser();
                    m_NextFetchCurrentUser = DateTime.Now.AddSeconds(60);
                }
                if (DateTime.Now.CompareTo(m_NextFetchModeration) >= 0)
                {
                    ApiPlayerModeration.FetchAllAgainstMe();
                    m_NextFetchModeration = DateTime.Now.AddSeconds(60);
                }
                if (DateTime.Now.CompareTo(m_NextFetchFavorite) >= 0)
                {
                    ApiFavorite.FetchList(ApiFavorite.FavoriteType.Friend);
                    m_NextFetchFavorite = DateTime.Now.AddSeconds(60);
                }
                if (DateTime.Now.CompareTo(m_NextUpdateLog) >= 0)
                {
                    VRChatLog.Update();
                    m_NextUpdateLog = DateTime.Now.AddSeconds(5);
                }
            }
            if (checkbox_discord_presence.Checked &&
                FindWindow("UnityWndClass", "VRChat") != IntPtr.Zero)
            {
                if (checkbox_show_location.Checked)
                {
                    Discord.SetPresence(VRChatLog.WorldName, VRChatLog.AccessTag);
                }
                else
                {
                    Discord.SetPresence(string.Empty, string.Empty);
                }
            }
            else
            {
                Discord.RemovePresence();
            }
        }

        //
        // Global
        //

        public List<InstanceInfo> GetInstanceList()
        {
            var result = new List<InstanceInfo>();
            var dic = new Dictionary<string, InstanceInfo>();
            foreach (var pair in m_Friends)
            {
                var user = pair.Value;
                if (!dic.TryGetValue(user.location, out InstanceInfo instance))
                {
                    instance = new InstanceInfo();
                }
                instance.Name = user.locationInfo;
                instance.Friends.Add(user.displayName);
                dic[user.location] = instance;
            }
            return result;
        }

        public int GetFriendsGroupIndex(string id)
        {
            if (m_FavoriteFriends.TryGetValue(id, out int group))
            {
                return (group >= 1 && group <= 3) ? group : 1;
            }
            if (m_Friends.ContainsKey(id) ||
                (m_CurrentUser != null &&
                 m_CurrentUser.friends != null &&
                 m_CurrentUser.friends.Contains(id)))
            {
                return 0;
            }
            return -1;
        }

        public void SetLeftHand()
        {
            nud_RX.Value = 90;
            nud_RY.Value = 90;
            nud_RZ.Value = -90;
            nud_TX.Value = -7;
            nud_TY.Value = -5;
            nud_TZ.Value = 6;
            nud_S3.Value = 20;
        }

        public void SetRightHand()
        {
            nud_RX.Value = -90;
            nud_RY.Value = -90;
            nud_RZ.Value = -90;
            nud_TX.Value = 7;
            nud_TY.Value = -5;
            nud_TZ.Value = 6;
            nud_S3.Value = 20;
        }

        public void ShowMessage(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    ShowMessage(message);
                }));
            }
            else
            {
                listview_log.BeginUpdate();
                while (listview_log.Items.Count >= 100)
                {
                    listview_log.Items.RemoveAt(0);
                }
                var item = new ListViewItem
                {
                    Text = DateTime.Now.ToString("HH:mm") + " " + message
                };
                listview_log.Items.Add(item);
                item.EnsureVisible();
                listview_log.EndUpdate();
                if (LoginForm.Instance != null)
                {
                    LoginForm.Instance.Reset();
                }
            }
        }

        public void OnLogin(ApiUser user = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    OnLogin(user);
                }));
            }
            else
            {
                m_CurrentUser = null;
                m_NextUpdateLog = DateTime.Now.AddSeconds(5);
                m_NextFetchVisits = DateTime.MinValue;
                m_NextFetchCurrentUser = DateTime.MinValue;
                m_NextFetchModeration = DateTime.MinValue;
                m_NextFetchFavorite = DateTime.MinValue;
                m_Moderation = null;
                m_Friends.Clear();
                m_FavoriteFriends.Clear();
                m_GPS.Clear();
                if (user == null)
                {
                    ApiUser.FetchCurrentUser();
                }
                else
                {
                    OnCurrentUser(user);
                }
            }
        }

        public void OnVisits(string data)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    OnVisits(data);
                }));
            }
            else
            {
                m_NextFetchVisits = DateTime.Now.AddSeconds(60);
                ShowMessage("Online User(s) : " + data);
            }
        }

        //
        // ApiModel
        //

        public void OnResponse(ApiResponse response)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    OnResponse(response);
                }));
            }
            else
            {
                if (response.error != null)
                {
                    var result = response.error;
                    ShowMessage($"code {result.status_code}, {result.message}");
                    if (result.status_code == 401)
                    {
                        m_CurrentUser = null;
                        label_login.Text = "Not logged in";
                        if (LoginForm.Instance == null)
                        {
                            new LoginForm().ShowDialog(this);
                            if (m_CurrentUser == null)
                            {
                                m_Shutdown = true;
                                Application.Exit();
                            }
                        }
                    }
                }
                if (response.success != null)
                {
                    var result = response.success;
                    ShowMessage($"code {result.status_code}, {result.message}");
                }
            }
        }

        //
        // ApiUser
        //

        public void OnCurrentUser(ApiUser user)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    OnCurrentUser(user);
                }));
            }
            else
            {
                LastLoginSuccess = true;
                m_NextFetchCurrentUser = DateTime.Now.AddSeconds(60);
                m_CurrentUser = user;
                ApiUser.FetchFriends();
                if (user.friends != null)
                {
                    var friends = user.friends;
                    var set = new HashSet<string>();
                    foreach (var pair in m_Friends)
                    {
                        if (!friends.Contains(pair.Key))
                        {
                            set.Add(pair.Key);
                            VRSEX.SetActivity(new ActivityInfo
                            {
                                Type = ActivityType.Moderation,
                                Text = DateTime.Now.ToString("HH:mm") + " " + pair.Value.displayName + " has unfriended you"
                            });
                        }
                    }
                    foreach (var id in set)
                    {
                        m_Friends.Remove(id);
                    }
                }
                label_login.Text = "Logged in as " + user.displayName + " (" + user.username + ")";
                if (LoginForm.Instance != null)
                {
                    LoginForm.Instance.Close();
                }
            }
        }

        public void OnFriends(List<ApiUser> users)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    OnFriends(users);
                }));
            }
            else
            {
                foreach (var user in users)
                {
                    if (m_Friends.TryGetValue(user.id, out ApiUser _user))
                    {
                        user.pastLocation = _user.location;
                    }
                    m_Friends[user.id] = user;
                    var notify = true;
                    var locationInfo = user.location;
                    LocationInfo L = LocationInfo.Parse(user.location);
                    if (L != null)
                    {
                        if (m_WorldNames.TryGetValue(L.WorldId, out string worldName))
                        {
                            locationInfo = $"{worldName} {L.InstanceInfo}";
                        }
                        else
                        {
                            notify = false;
                            if (m_GPS.Add(L.WorldId))
                            {
                                ApiWorld.Fetch(L.WorldId);
                            }
                        }
                    }
                    user.locationInfo = locationInfo;
                    if (!string.IsNullOrEmpty(user.pastLocation) &&
                        !string.Equals(user.pastLocation, user.location))
                    {
                        if ("offline".Equals(user.location, StringComparison.OrdinalIgnoreCase))
                        {
                            VRSEX.SetActivity(new ActivityInfo
                            {
                                Type = ActivityType.PlayerLogout,
                                Text = DateTime.Now.ToString("HH:mm") + " " + user.displayName + " has logged out",
                                Group = GetFriendsGroupIndex(user.id)
                            });
                        }
                        else
                        {
                            if ("offline".Equals(user.pastLocation, StringComparison.OrdinalIgnoreCase))
                            {
                                VRSEX.SetActivity(new ActivityInfo
                                {
                                    Type = ActivityType.PlayerLogin,
                                    Text = DateTime.Now.ToString("HH:mm") + " " + user.displayName + " has logged in",
                                    Group = GetFriendsGroupIndex(user.id)
                                });
                                if ("private".Equals(user.location, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                            }
                            if (notify)
                            {
                                VRSEX.SetActivity(new ActivityInfo
                                {
                                    Type = ActivityType.PlayerGPS,
                                    Text = DateTime.Now.ToString("HH:mm") + " " + user.displayName + " is " + locationInfo,
                                    Group = GetFriendsGroupIndex(user.id)
                                });
                            }
                        }
                    }
                }
            }
        }

        //
        // ApiWorld
        //

        public void OnWorld(ApiWorld world)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    OnWorld(world);
                }));
            }
            else
            {
                m_WorldNames[world.id] = world.name;
                if (m_GPS.Remove(world.id))
                {
                    foreach (var pair in m_Friends)
                    {
                        var user = pair.Value;
                        var L = LocationInfo.Parse(user.location);
                        if (L != null &&
                            L.WorldId.Equals(world.id))
                        {
                            var locationInfo = $"{world.name} {L.InstanceInfo}";
                            user.locationInfo = locationInfo;
                            if (!string.IsNullOrEmpty(user.pastLocation))
                            {
                                VRSEX.SetActivity(new ActivityInfo
                                {
                                    Type = ActivityType.PlayerGPS,
                                    Text = DateTime.Now.ToString("HH:mm") + " " + user.displayName + " is " + locationInfo,
                                    Group = GetFriendsGroupIndex(user.id)
                                });
                            }
                        }
                    }
                }
            }
        }

        //
        // ApiFavorite
        //

        public void OnFavorites(ApiFavorite.FavoriteType type, List<ApiFavorite> favorites)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    OnFavorites(type, favorites);
                }));
            }
            else if (type == ApiFavorite.FavoriteType.Friend)
            {
                m_FavoriteFriends.Clear();
                foreach (var favorite in favorites)
                {
                    var group = 0;
                    if (favorite.tags != null)
                    {
                        if (favorite.tags.Contains("group_0"))
                        {
                            group = 1;
                        }
                        else if (favorite.tags.Contains("group_1"))
                        {
                            group = 2;
                        }
                        else if (favorite.tags.Contains("group_2"))
                        {
                            group = 3;
                        }
                    }
                    m_FavoriteFriends[favorite.favoriteId] = group;
                }
            }
        }

        //
        // ApiPlayerModeration
        //

        public void OnPlayerModerationsAgainstMe(List<ApiPlayerModeration> moderations)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() =>
                {
                    OnPlayerModerationsAgainstMe(moderations);
                }));
            }
            else
            {
                m_NextFetchModeration = DateTime.Now.AddSeconds(60);
                var dic = new Dictionary<string, ApiPlayerModeration>();
                foreach (var moderation in moderations)
                {
                    if (m_Moderation != null &&
                        !m_Moderation.Remove(moderation.id))
                    {
                        if ("block".Equals(moderation.type, StringComparison.OrdinalIgnoreCase))
                        {
                            VRSEX.SetActivity(new ActivityInfo
                            {
                                Type = ActivityType.Moderation,
                                Text = DateTime.Now.ToString("HH:mm") + " " + moderation.sourceDisplayName + " has blocked you"
                            });
                        }
                        else if ("mute".Equals(moderation.type, StringComparison.OrdinalIgnoreCase))
                        {
                            VRSEX.SetActivity(new ActivityInfo
                            {
                                Type = ActivityType.Moderation,
                                Text = DateTime.Now.ToString("HH:mm") + " " + moderation.sourceDisplayName + " has muted you"
                            });
                        }
                        else if ("hideAvatar".Equals(moderation.type, StringComparison.OrdinalIgnoreCase))
                        {
                            VRSEX.SetActivity(new ActivityInfo
                            {
                                Type = ActivityType.Moderation,
                                Text = DateTime.Now.ToString("HH:mm") + " " + moderation.sourceDisplayName + " has hidden you"
                            });
                        }
                    }
                    dic[moderation.id] = moderation;
                }
                if (m_Moderation != null)
                {
                    foreach (var pair in m_Moderation)
                    {
                        var moderation = pair.Value;
                        if ("block".Equals(moderation.type, StringComparison.OrdinalIgnoreCase))
                        {
                            VRSEX.SetActivity(new ActivityInfo
                            {
                                Type = ActivityType.Moderation,
                                Text = DateTime.Now.ToString("HH:mm") + " " + moderation.sourceDisplayName + " has unblocked you"
                            });
                        }
                        else if ("mute".Equals(moderation.type, StringComparison.OrdinalIgnoreCase))
                        {
                            VRSEX.SetActivity(new ActivityInfo
                            {
                                Type = ActivityType.Moderation,
                                Text = DateTime.Now.ToString("HH:mm") + " " + moderation.sourceDisplayName + " has unmuted you"
                            });
                        }
                        else if ("hideAvatar".Equals(moderation.type, StringComparison.OrdinalIgnoreCase))
                        {
                            VRSEX.SetActivity(new ActivityInfo
                            {
                                Type = ActivityType.Moderation,
                                Text = DateTime.Now.ToString("HH:mm") + " " + moderation.sourceDisplayName + " has showed you"
                            });
                        }
                    }
                }
                m_Moderation = dic;
            }
        }

        private void label_link_DoubleClick(object sender, EventArgs e)
        {
            Process.Start("https://gall.dcinside.com/m/list.php?id=vr");
        }
    }
}