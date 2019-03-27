using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace VRSEX
{
    public enum ApiMethod
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    public class ApiResult
    {
        public string message;
        public int status_code;
    }

    public class ApiResponse
    {
        public ApiResult error;
        public ApiResult success;
    }

    //
    // VRChat
    //

    public static class VRCApi
    {
        private static readonly string API_URL = "https://api.vrchat.cloud/api/1/";
        private static readonly string COOKIE_FILE_NAME = "cookie.dat";
        private static string m_ApiKey = string.Empty;
        private static CookieContainer m_CookieContainer = new CookieContainer();

        static VRCApi()
        {
            RemoteConfig.FetchConfig((config) =>
            {
                m_ApiKey = config.apiKey;
            });
        }

        public static void ClearCookie()
        {
            m_CookieContainer = new CookieContainer();
        }

        public static void LoadCookie()
        {
            Utils.Deserialize(COOKIE_FILE_NAME, ref m_CookieContainer);
        }

        public static void SaveCookie()
        {
            Utils.Serialize(COOKIE_FILE_NAME, m_CookieContainer);
        }

        private static void HandleJson<T>(HttpWebResponse response, Action<T> callback)
        {
            using (var stream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        callback.Invoke(new JsonSerializer().Deserialize<T>(jsonReader));
                    }
                }
            }
        }

        public static async void Request<T>(Action<T> callback, string endpoint, ApiMethod method = ApiMethod.GET, Dictionary<string, object> data = null, Action<HttpWebRequest> setup = null)
        {
            try
            {
                var uri = new UriBuilder(API_URL + endpoint);
                var query = new StringBuilder(uri.Query);
                if (query.Length > 0)
                {
                    query.Remove(0, 1);
                }
                if (method == ApiMethod.GET &&
                    data != null)
                {
                    foreach (var pair in data)
                    {
                        if (query.Length > 0)
                        {
                            query.Append('&');
                        }
                        query.Append(pair.Key);
                        query.Append('=');
                        if (pair.Value != null)
                        {
                            query.Append(Uri.EscapeDataString(pair.Value.ToString()));
                        }
                    }
                }
                if (!string.IsNullOrEmpty(m_ApiKey) &&
                    !"config".Equals(uri.Path))
                {
                    query.Insert(0, "apiKey=" + m_ApiKey + (query.Length > 0 ? "&" : string.Empty));
                }
                uri.Query = query.ToString();
                var request = WebRequest.CreateHttp(uri.Uri);
                request.CookieContainer = m_CookieContainer;
                request.KeepAlive = true;
                request.Method = method.ToString();
                if (setup != null)
                {
                    setup.Invoke(request);
                }
                if (method != ApiMethod.GET &&
                    data != null)
                {
                    request.ContentType = "application/json;charset=utf-8";
                    using (var stream = await request.GetRequestStreamAsync())
                    {
                        using (var writer = new StreamWriter(stream))
                        {
                            using (var jsonWriter = new JsonTextWriter(writer))
                            {
                                new JsonSerializer().Serialize(jsonWriter, data);
                            }
                        }
                    }
                }
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    HandleJson(response, callback);
                }
            }
            catch (WebException w)
            {
                try
                {
                    using (var response = w.Response as HttpWebResponse)
                    {
                        if (response.ContentType != null && response.ContentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) != -1)
                        {
                            HandleJson<ApiResponse>(response, MainForm.Instance.OnResponse);
                        }
                        else
                        {
                            MainForm.Instance.ShowMessage(endpoint + ": " + w.Message);
                        }
                    }
                }
                catch (Exception x)
                {
                    MainForm.Instance.ShowMessage(endpoint + ": " + x.Message);
                }
            }
            catch (Exception x)
            {
                MainForm.Instance.ShowMessage(endpoint + ": " + x.Message);
            }
        }

        public static void Login(string username, string password)
        {
            ClearCookie();
            Request<ApiUser>(MainForm.Instance.OnLogin, "auth/user", ApiMethod.GET, null, (request) =>
            {
                request.Headers["Authorization"] = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))}";
            });
        }

        public static void ThridPartyLogin(string endpoint, Dictionary<string, object> param)
        {
            ClearCookie();
            Request<ApiUser>(MainForm.Instance.OnLogin, $"auth/{endpoint}", ApiMethod.POST, param);
        }

        public static async void FetchVisits()
        {
            try
            {
                var request = WebRequest.CreateHttp(API_URL + "visits");
                request.KeepAlive = true;
                request.Method = "GET";
                using (var response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            if (int.TryParse(reader.ReadToEnd(), out int n))
                            {
                                MainForm.Instance.OnVisits(n.ToString());
                            }
                        }
                    }
                }
            }
            catch (WebException w)
            {
                MainForm.Instance.ShowMessage(w.Message);
            }
            catch (Exception x)
            {
                MainForm.Instance.ShowMessage(x.Message);
            }
        }
    }

    //
    // RemoteConfig
    //

    public class RemoteConfig
    {
        public string apiKey;

        public static void FetchConfig(Action<RemoteConfig> callback)
        {
            VRCApi.Request(callback, "config");
        }
    }

    //
    // ApiUser
    //

    public class ApiUser
    {
        public string id;
        public string displayName;
        public string username;
        public string location;
        public HashSet<string> friends;
        public HashSet<string> tags;
        public bool HasTag(string tag) => tags != null && tags.Contains(tag);
        public List<string> friendGroupNames;

        // VRCEX variable
        public string locationInfo = string.Empty;
        public string pastLocation = string.Empty;

        public static void FetchCurrentUser()
        {
            VRCApi.Request<ApiUser>(MainForm.Instance.OnCurrentUser, "auth/user");
        }

        private static void FetchFriends(Action<List<ApiUser>> callback, Dictionary<string, object> data, int count = 100, int offset = 0)
        {
            data["n"] = count;
            data["offset"] = offset;
            VRCApi.Request<List<ApiUser>>((list) =>
            {
                if (list.Count == count)
                {
                    FetchFriends(callback, data, count, offset + count);
                }
                callback.Invoke(list);
            }, "auth/user/friends", ApiMethod.GET, data);
        }

        public static void FetchFriends()
        {
            FetchFriends(MainForm.Instance.OnFriends, new Dictionary<string, object>());
            FetchFriends(MainForm.Instance.OnFriends, new Dictionary<string, object>()
            {
                ["offline"] = "true"
            });
        }

        public string GetFriendsGroupDisplayName(int index)
        {
            if (friendGroupNames != null && index < 3 && index < friendGroupNames.Count && !string.IsNullOrEmpty(friendGroupNames[index]))
            {
                return friendGroupNames[index];
            }
            return "Group " + (index + 1);
        }
    }

    //
    // ApiWorld
    //

    public class ApiWorld
    {
        public string id;
        public string name;

        public static void Fetch(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                VRCApi.Request<ApiWorld>(MainForm.Instance.OnWorld, $"worlds/{id}");
            }
        }
    }

    //
    // ApiFavorite
    //

    public class ApiFavorite
    {
        public enum FavoriteType
        {
            Friend,
            World,
            Avatar
        }

        public string id;
        public string type;
        public string favoriteId;
        public HashSet<string> tags;

        public static void FetchList(FavoriteType type, string tags = null)
        {
            var param = new Dictionary<string, object>
            {
                ["n"] = 100,
                ["type"] = type.ToString().ToLower()
            };
            if (!string.IsNullOrEmpty(tags))
            {
                param["tags"] = tags;
            }
            VRCApi.Request<List<ApiFavorite>>((list) => MainForm.Instance.OnFavorites(type, list), "favorites", ApiMethod.GET, param);
        }
    }

    //
    // ApiPlayerModeration
    //

    public class ApiPlayerModeration
    {
        public string id;
        public string type;
        public string sourceUserId;
        public string sourceDisplayName;

        public static void FetchAllAgainstMe()
        {
            VRCApi.Request<List<ApiPlayerModeration>>(MainForm.Instance.OnPlayerModerationsAgainstMe, "auth/user/playermoderated");
        }
    }

    //
    // LocationInfo
    //

    public class LocationInfo
    {
        public string WorldId = string.Empty;
        public string InstanceId = string.Empty;
        public string InstanceInfo = string.Empty;

        public static LocationInfo Parse(string idWithTags, bool strict = true)
        {
            // offline
            // private
            // local:0000000000
            // Public       wrld_785bee79-b83b-449c-a3d9-f1c5a29bcd3d:12502
            // Friends+     wrld_785bee79-b83b-449c-a3d9-f1c5a29bcd3d:12502~hidden(usr_4f76a584-9d4b-46f6-8209-8305eb683661)~nonce(79985ba6-8804-49dd-8c8a-c86fe817caca)
            // Friends      wrld_785bee79-b83b-449c-a3d9-f1c5a29bcd3d:12502~friends(usr_4f76a584-9d4b-46f6-8209-8305eb683661)~nonce(13374166-629e-4ac5-afe9-29637719d78c)
            // Invite+      wrld_785bee79-b83b-449c-a3d9-f1c5a29bcd3d:12502~private(usr_4f76a584-9d4b-46f6-8209-8305eb683661)~nonce(6d9b02ca-f32c-4360-b8ac-9996bf12fd74)~canRequestInvite
            // Invite       wrld_785bee79-b83b-449c-a3d9-f1c5a29bcd3d:12502~private(usr_4f76a584-9d4b-46f6-8209-8305eb683661)~nonce(5db0f688-4211-428b-83c5-91533e0a5d5d)
            // wrld_가 아니라 wld_인 것들도 있고 예전 맵들의 경우 아예 o_나 b_인것들도 있음; 그냥 uuid형태인 것들도 있고 개판임
            var tags = idWithTags.Split('~');
            var a = tags[0].Split(new char[] { ':' }, 2);
            if (!string.IsNullOrEmpty(a[0]))
            {
                if (a.Length == 2)
                {
                    if (!string.IsNullOrEmpty(a[1]) &&
                        !"local".Equals(a[0]))
                    {
                        var L = new LocationInfo
                        {
                            WorldId = a[0]
                        };
                        var type = "public";
                        if (tags.Length > 1)
                        {
                            var tag = "~" + string.Join("~", tags, 1, tags.Length - 1);
                            if (tag.Contains("~private("))
                            {
                                if (tag.Contains("~canRequestInvite"))
                                {
                                    type = "invite+"; // Invite Plus
                                }
                                else
                                {
                                    type = "invite"; // Invite Only
                                }
                            }
                            else if (tag.Contains("~friends("))
                            {
                                type = "friends"; // Friends Only
                            }
                            else if (tag.Contains("~hidden("))
                            {
                                type = "friends+"; // Friends of Guests
                            }
                            L.InstanceId = a[1] + tag;
                        }
                        else
                        {
                            L.InstanceId = a[1];
                        }
                        L.InstanceInfo = $"#{a[1]} {type}";
                        return L;
                    }
                }
                else if (!strict && !("offline".Equals(a[0]) || "private".Equals(a[0])))
                {
                    return new LocationInfo()
                    {
                        WorldId = a[0]
                    };
                }
            }
            return null;
        }
    }

    //
    // VRChatLog
    //

    public static class VRChatLog
    {
        public static string AccessTag { get; private set; } = string.Empty;
        public static string WorldName { get; private set; } = string.Empty;
        public static int InRoom { get { return m_InRoom.Count; } }

        private static long m_Position = 0;
        private static string m_FirstLog = string.Empty;
        private static string m_CurrentUser = string.Empty;
        private static string m_Location = string.Empty;
        private static string m_UserID = string.Empty;
        private static Dictionary<string, string> m_InRoom = new Dictionary<string, string>();

        public static void Update()
        {
            try
            {
                var directory = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"Low\VRChat\VRChat");
                if (directory != null && directory.Exists)
                {
                    FileInfo target = null;
                    foreach (var info in directory.GetFiles("output_log_*.txt", SearchOption.TopDirectoryOnly))
                    {
                        if (target == null || info.LastAccessTime.CompareTo(target.LastAccessTime) >= 0)
                        {
                            target = info;
                        }
                    }
                    if (target != null)
                    {
                        using (var file = target.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var stream = new BufferedStream(file, 64 * 1024))
                            {
                                using (var reader = new StreamReader(stream, Encoding.UTF8))
                                {
                                    var line = string.Empty;
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        if (line.Length > 34 &&
                                            line[20] == 'L' &&
                                            line[21] == 'o' &&
                                            line[22] == 'g' &&
                                            line[31] == '-')
                                        {
                                            var s = line.Substring(0, 19);
                                            if (m_FirstLog.Equals(s))
                                            {
                                                stream.Position = m_Position;
                                            }
                                            else
                                            {
                                                m_FirstLog = s;
                                            }
                                            Parse(line);
                                            break;
                                        }
                                    }
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        if (line.Length > 34 &&
                                            line[20] == 'L' &&
                                            line[21] == 'o' &&
                                            line[22] == 'g' &&
                                            line[31] == '-')
                                        {
                                            Parse(line);
                                        }
                                    }
                                    m_Position = stream.Position;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {

            }
        }

        private static void Parse(string line)
        {
            try
            {
                if (line[34] == '[')
                {
                    if (line[46] == ']')
                    {
                        var s = line.Substring(34);
                        if (s.StartsWith("[RoomManager] Joining "))
                        {
                            if (s.StartsWith("[RoomManager] Joining or Creating Room: "))
                            {
                                s = line.Substring(74);
                                if (!string.IsNullOrEmpty(m_Location))
                                {
                                    WorldName = s;
                                    var L = LocationInfo.Parse(m_Location);
                                    if (L != null)
                                    {
                                        AccessTag = L.InstanceInfo;
                                        s += " " + L.InstanceInfo;
                                    }
                                    else
                                    {
                                        AccessTag = string.Empty;
                                    }
                                    VRSEX.SetActivity(new ActivityInfo
                                    {
                                        Type = ActivityType.EnterWorld,
                                        Text = line.Substring(11, 5) + " " + s,
                                        Tag = m_Location
                                    });
                                    m_InRoom.Clear();
                                    m_Location = string.Empty;
                                }
                            }
                            else
                            {
                                m_Location = line.Substring(56);
                            }
                        }
                    }
                    else if (line[49] == ']')
                    {
                        var s = line.Substring(34);
                        if (s.StartsWith("[NetworkManager] OnPlayerLeft "))
                        {
                            s = line.Substring(64);
                            if (m_InRoom.TryGetValue(s, out string id))
                            {
                                VRSEX.SetActivity(new ActivityInfo
                                {
                                    Type = ActivityType.PlayerLeft,
                                    Text = line.Substring(11, 5) + " " + s + " has left",
                                    Tag = s,
                                    Group = MainForm.Instance.GetFriendsGroupIndex(id)
                                });
                                m_InRoom.Remove(s);
                            }
                        }
                    }
                    else if (line[52] == ']')
                    {
                        var s = line.Substring(34);
                        if (s.StartsWith("[VRCFlowManagerVRC] User Authenticated: "))
                        {
                            m_CurrentUser = line.Substring(74);
                        }
                    }
                }
                else if (line[34] == 'R')
                {
                    var s = line.Substring(34);
                    if (s.StartsWith("Received API user "))
                    {
                        m_UserID = line.Substring(52);
                    }
                }
                else if (line[34] == 'S')
                {
                    var s = line.Substring(34);
                    if (s.StartsWith("Switching "))
                    {
                        var i = s.IndexOf(" to avatar ");
                        if (i > 10)
                        {
                            s = s.Substring(10, i - 10);
                            if (!string.IsNullOrEmpty(m_UserID))
                            {
                                if (!m_InRoom.ContainsKey(s))
                                {
                                    VRSEX.SetActivity(new ActivityInfo
                                    {
                                        Type = ActivityType.PlayerJoined,
                                        Text = line.Substring(11, 5) + " " + s + " has joined",
                                        Tag = s,
                                        Group = MainForm.Instance.GetFriendsGroupIndex(m_UserID)
                                    });
                                }
                                m_InRoom[s] = m_UserID;
                                m_UserID = string.Empty;
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}