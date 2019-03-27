using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;
using Valve.VR;
using Device = SharpDX.Direct3D11.Device;

namespace VRSEX
{
    public enum ActivityType
    {
        None,
        EnterWorld,
        PlayerJoined,
        PlayerLeft,
        PlayerLogin,
        PlayerLogout,
        PlayerGPS,
        Moderation
    }

    public class ActivityInfo
    {
        public ActivityType Type = ActivityType.None;
        public string Text = string.Empty;
        public string Tag = string.Empty;
        public int Group = -1;
    }

    public class NotifyInfo
    {
        public DateTime Expire = DateTime.Now.AddSeconds(5);
        public string Text = string.Empty;
    }

    public class InstanceInfo
    {
        public string Name;
        public List<string> Friends;
    }

    public class VRDeviceInfo
    {
        public uint Index;
        public bool Connected;
        public ETrackedDeviceClass Class;
        public ETrackedControllerRole Role;
        public float BatteryPercentage;
        public bool Oculus;

        public int GetPriority()
        {
            var value = (int)(OpenVR.k_unMaxTrackedDeviceCount - Index);
            switch (Class)
            {
                case ETrackedDeviceClass.HMD:
                    value += 64000;
                    break;
                case ETrackedDeviceClass.Controller:
                    value += 32000;
                    break;
                case ETrackedDeviceClass.GenericTracker:
                    value += 16000;
                    break;
                case ETrackedDeviceClass.TrackingReference:
                    value += 8000;
                    break;
            }
            switch (Role)
            {
                case ETrackedControllerRole.LeftHand:
                    value += 4000;
                    break;
                case ETrackedControllerRole.RightHand:
                    value += 2000;
                    break;
            }
            return value;
        }
    }

    public class VRSEX
    {
        public static readonly string APP = "VRSEX v0.03c";
        private const int XS = 1024;
        private const int YS = 1536;

        private static RenderForm m_RenderForm;

        private static Device m_Device;
        private static SwapChain m_SwapChain;
        private static Texture2D m_BackBuffer;
        private static Texture2D m_BackBuffer2;
        private static RenderTarget m_RenderTarget;
        private static RenderTarget m_RenderTarget2;
        private static uint m_RenderDeviceIndex = OpenVR.k_unTrackedDeviceIndexInvalid;
        private static DateTime m_NextVRSetup = DateTime.MinValue;
        private static DateTime m_NextVRUpdate = DateTime.MinValue;
        private static DateTime m_NextVRRender = DateTime.MinValue;
        private static List<VRDeviceInfo> m_DeviceInfos = new List<VRDeviceInfo>();
        private static List<ActivityInfo> m_ActivityInfos = new List<ActivityInfo>();
        private static List<NotifyInfo> m_NotifyInfos = new List<NotifyInfo>();

        public static void SetActivity(ActivityInfo info)
        {
            while (m_ActivityInfos.Count >= 100)
            {
                m_ActivityInfos.RemoveAt(0);
            }
            m_ActivityInfos.Add(info);
            if ((info.Type == ActivityType.Moderation ||
                (info.Group > 0 &&
                (info.Type == ActivityType.PlayerLogin ||
                info.Type == ActivityType.PlayerLogout ||
                info.Type == ActivityType.PlayerJoined ||
                info.Type == ActivityType.PlayerLeft))) &&
                "true".Equals(LocalConfig.GetString("Overlay", "true"), StringComparison.OrdinalIgnoreCase))
            {
                var s = info.Text;
                if (s.Length > 6)
                {
                    s = s.Substring(6);
                }
                SetNotify(new NotifyInfo
                {
                    Text = s
                });
            }
        }

        public static void SetNotify(NotifyInfo info)
        {
            while (m_NotifyInfos.Count >= 10)
            {
                m_NotifyInfos.RemoveAt(0);
            }
            m_NotifyInfos.Add(info);
        }

        private static void UpdateVR()
        {
            var system = OpenVR.System;
            if (system == null &&
                DateTime.Now.CompareTo(m_NextVRSetup) >= 0)
            {
                var err = EVRInitError.None;
                system = OpenVR.Init(ref err, EVRApplicationType.VRApplication_Overlay);
                m_NextVRSetup = DateTime.Now.AddSeconds(5);
            }
            if (system != null)
            {
                var _event = new VREvent_t();
                while (system.PollNextEvent(ref _event, (uint)Marshal.SizeOf(typeof(VREvent_t))))
                {
                    if ((EVREventType)_event.eventType == EVREventType.VREvent_Quit)
                    {
                        OpenVR.Shutdown();
                        m_NextVRSetup = DateTime.Now.AddSeconds(30);
                        return;
                    }
                }
                if (DateTime.Now.CompareTo(m_NextVRUpdate) >= 0)
                {
                    m_DeviceInfos.Clear();
                    var b = new StringBuilder(256);
                    var state = new VRControllerState_t();
                    for (var i = 0u; i < OpenVR.k_unMaxTrackedDeviceCount; ++i)
                    {
                        var _class = system.GetTrackedDeviceClass(i);
                        if (_class == ETrackedDeviceClass.Controller ||
                            _class == ETrackedDeviceClass.GenericTracker)
                        {
                            var err = ETrackedPropertyError.TrackedProp_Success;
                            var bp = system.GetFloatTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_DeviceBatteryPercentage_Float, ref err);
                            if (err != ETrackedPropertyError.TrackedProp_Success)
                            {
                                bp = 1;
                            }
                            b.Clear();
                            system.GetStringTrackedDeviceProperty(i, ETrackedDeviceProperty.Prop_TrackingSystemName_String, b, (uint)b.Capacity, ref err);
                            var is_oculus = b.ToString().IndexOf("oculus", StringComparison.OrdinalIgnoreCase) >= 0;
                            // Oculus : B/Y, Bit 1, Mask 2
                            // Oculus : A/X, Bit 7, Mask 128
                            // Vive : Menu, Bit 1, Mask 2,
                            // Vive : Grip, Bit 2, Mask 4
                            var role = system.GetControllerRoleForTrackedDeviceIndex(i);
                            if (role == ETrackedControllerRole.LeftHand ||
                                role == ETrackedControllerRole.RightHand)
                            {
                                if (system.GetControllerState(i, ref state, (uint)Marshal.SizeOf(typeof(VREvent_t))))
                                {
                                    if ((state.ulButtonPressed & (is_oculus ? 2u : 4u)) != 0)
                                    {
                                        if (role == ETrackedControllerRole.LeftHand)
                                        {
                                            MainForm.Instance.SetLeftHand();
                                        }
                                        else
                                        {
                                            MainForm.Instance.SetRightHand();
                                        }
                                        m_RenderDeviceIndex = i;
                                        m_NextVRRender = DateTime.Now.AddSeconds(10);
                                    }
                                }
                            }
                            m_DeviceInfos.Add(new VRDeviceInfo
                            {
                                Index = i,
                                Connected = system.IsTrackedDeviceConnected(i),
                                Class = _class,
                                Role = role,
                                BatteryPercentage = bp,
                                Oculus = is_oculus
                            });
                        }
                    }
                    m_DeviceInfos.Sort((_a, _b) => _b.GetPriority() - _a.GetPriority());
                    m_NextVRUpdate = DateTime.Now.AddSeconds(0.1);
                }
            }
        }

        private static void RenderVR()
        {
            var overlay = OpenVR.Overlay;
            if (overlay != null)
            {
                ulong handle = 0;
                /*ulong thumbnail_handle = 0;
                if (overlay.FindOverlay("VRSEX0", ref handle) != EVROverlayError.None &&
                    overlay.CreateDashboardOverlay("VRSEX0", "VRSEX", ref handle, ref thumbnail_handle) == EVROverlayError.None)
                {
                    overlay.SetOverlayWidthInMeters(handle, 1.5f);
                    overlay.SetOverlayInputMethod(handle, VROverlayInputMethod.DualAnalog);
                    overlay.ClearOverlayTexture(handle);
                }
                if (handle != 0)
                {
                    var _event = new VREvent_t();
                    while (overlay.PollNextOverlayEvent(handle, ref _event, (uint)Marshal.SizeOf(typeof(VREvent_t))))
                    {
                        switch ((EVREventType)_event.eventType)
                        {
                            case EVREventType.VREvent_MouseMove:
                                //Console.WriteLine("M {0} ({1},{2})", _event.data.mouse.button, _event.data.mouse.x, _event.data.mouse.y);
                                continue;
                            case EVREventType.VREvent_DualAnalog_Move:
                                //Console.WriteLine("T {0} ({1},{2})", _event.data.dualAnalog.which, _event.data.dualAnalog.x, _event.data.dualAnalog.y);
                                continue;
                        }
                        Console.WriteLine("Overlay0 {0}", (EVREventType)_event.eventType);
                    }
                    var texture = new Texture_t
                    {
                        handle = m_BackBuffer.NativePointer
                    };
                    overlay.SetOverlayTexture(handle, ref texture);
                }*/
                handle = 0;
                if (overlay.FindOverlay("VRSEX1", ref handle) != EVROverlayError.None &&
                    overlay.CreateOverlay("VRSEX1", "VRSEX1", ref handle) == EVROverlayError.None)
                {
                    overlay.SetOverlayAlpha(handle, 0.95f);
                    overlay.SetOverlayWidthInMeters(handle, 1f);
                    overlay.SetOverlayInputMethod(handle, VROverlayInputMethod.None);
                    overlay.ClearOverlayTexture(handle);
                }
                if (handle != 0)
                {
                    if (DateTime.Now.CompareTo(m_NextVRRender) >= 0)
                    {
                        overlay.HideOverlay(handle);
                    }
                    else
                    {
                        if (m_RenderDeviceIndex != OpenVR.k_unTrackedDeviceIndexInvalid)
                        {
                            // http://www.opengl-tutorial.org/beginners-tutorials/tutorial-3-matrices
                            // Scaling-Rotation-Translation
                            var m = Matrix.Scaling(MainForm.Instance.ScaleXYZ);
                            m *= Matrix.RotationX(MainForm.Instance.RotationX);
                            m *= Matrix.RotationY(MainForm.Instance.RotationY);
                            m *= Matrix.RotationZ(MainForm.Instance.RotationZ);
                            m *= Matrix.Translation(MainForm.Instance.X, MainForm.Instance.Y, MainForm.Instance.Z);
                            var hm34 = new HmdMatrix34_t
                            {
                                m0 = m.M11,
                                m1 = m.M21,
                                m2 = m.M31,
                                m3 = m.M41,
                                m4 = m.M12,
                                m5 = m.M22,
                                m6 = m.M32,
                                m7 = m.M42,
                                m8 = m.M13,
                                m9 = m.M23,
                                m10 = m.M33,
                                m11 = m.M43,
                            };
                            overlay.SetOverlayTransformTrackedDeviceRelative(handle, m_RenderDeviceIndex, ref hm34);
                        }
                        var texture = new Texture_t
                        {
                            handle = m_BackBuffer.NativePointer
                        };
                        overlay.SetOverlayTexture(handle, ref texture);
                        overlay.ShowOverlay(handle);
                    }
                }
                handle = 0;
                if (overlay.FindOverlay("VRSEX2", ref handle) != EVROverlayError.None &&
                    overlay.CreateOverlay("VRSEX2", "VRSEX2", ref handle) == EVROverlayError.None)
                {
                    overlay.SetOverlayAlpha(handle, 0.5f);
                    overlay.SetOverlayWidthInMeters(handle, 1f);
                    overlay.SetOverlayInputMethod(handle, VROverlayInputMethod.None);
                    overlay.ClearOverlayTexture(handle);
                }
                if (handle != 0)
                {
                    var m = Matrix.Scaling(1f);
                    m *= Matrix.Translation(0, -0.5f, -2f);
                    var hm34 = new HmdMatrix34_t
                    {
                        m0 = m.M11,
                        m1 = m.M21,
                        m2 = m.M31,
                        m3 = m.M41,
                        m4 = m.M12,
                        m5 = m.M22,
                        m6 = m.M32,
                        m7 = m.M42,
                        m8 = m.M13,
                        m9 = m.M23,
                        m10 = m.M33,
                        m11 = m.M43,
                    };
                    overlay.SetOverlayTransformTrackedDeviceRelative(handle, OpenVR.k_unTrackedDeviceIndex_Hmd, ref hm34);
                    var texture = new Texture_t
                    {
                        handle = m_BackBuffer2.NativePointer
                    };
                    overlay.SetOverlayTexture(handle, ref texture);
                    overlay.ShowOverlay(handle);
                }
            }
        }

        // Transform pixels from BGRA to RGBA
        private static Bitmap LoadBitmap(RenderTarget target, System.Drawing.Bitmap bitmap)
        {
            var xs = bitmap.Width;
            var ys = bitmap.Height;
            var stride = xs * sizeof(int);
            using (var stream = new DataStream(ys * stride, true, true))
            {
                var bits = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, xs, ys), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                for (var y = 0; y < ys; ++y)
                {
                    var offset = bits.Stride * y;
                    for (var x = 0; x < xs; ++x)
                    {
                        // Not optimized
                        var B = Marshal.ReadByte(bits.Scan0, offset++);
                        var G = Marshal.ReadByte(bits.Scan0, offset++);
                        var R = Marshal.ReadByte(bits.Scan0, offset++);
                        var A = Marshal.ReadByte(bits.Scan0, offset++);
                        stream.Write(R | (G << 8) | (B << 16) | (A << 24));
                    }
                }
                bitmap.UnlockBits(bits);
                stream.Position = 0;
                return new Bitmap(target, new Size2(xs, ys), stream, stride, new BitmapProperties(new PixelFormat(Format.R8G8B8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Premultiplied)));
            }
        }

        private static void DrawBitmap(RenderTarget target, Bitmap bitmap, float x, float y)
        {
            target.DrawBitmap(bitmap, new RawRectangleF(x, y, x + bitmap.Size.Width, y + bitmap.Size.Height), 1, BitmapInterpolationMode.Linear);
        }

        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            PerformanceMonitor.Start();
            Discord.Start();
            LocalConfig.LoadConfig();
            VRCApi.LoadCookie();

            new MainForm();
            MainForm.Instance.Show();
            Application.DoEvents();

            m_RenderForm = new RenderForm
            {
                AllowUserResizing = false,
                ClientSize = new System.Drawing.Size(512, 768),
                Icon = null,
                Text = APP
            };
            Application.DoEvents();

            Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.BgraSupport, new[] {
                SharpDX.Direct3D.FeatureLevel.Level_10_0
            }, new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(XS, YS, new Rational(30, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = m_RenderForm.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            }, out m_Device, out m_SwapChain);
            m_SwapChain.GetParent<SharpDX.DXGI.Factory>().MakeWindowAssociation(m_RenderForm.Handle, WindowAssociationFlags.IgnoreAll);
            m_BackBuffer = Texture2D.FromSwapChain<Texture2D>(m_SwapChain, 0);
            m_BackBuffer2 = new Texture2D(m_Device, new Texture2DDescription()
            {
                Width = 1024,
                Height = 1024,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.R8G8B8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.RenderTarget
            });

            var factory2D = new SharpDX.Direct2D1.Factory();
            using (var surface = m_BackBuffer.QueryInterface<Surface>())
            {
                m_RenderTarget = new RenderTarget(factory2D, surface, new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied)))
                {
                    TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Cleartype,
                    AntialiasMode = AntialiasMode.PerPrimitive
                };
            }
            using (var surface = m_BackBuffer2.QueryInterface<Surface>())
            {
                m_RenderTarget2 = new RenderTarget(factory2D, surface, new RenderTargetProperties(new PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied)))
                {
                    TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Cleartype,
                    AntialiasMode = AntialiasMode.PerPrimitive
                };
            }

            // fonts
            var factoryDW = new SharpDX.DirectWrite.Factory();
            var textFormat1 = new TextFormat(factoryDW, "Arial", FontWeight.Bold, FontStyle.Normal, 64)
            {
                WordWrapping = WordWrapping.NoWrap,
                ParagraphAlignment = ParagraphAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            var textFormat2 = new TextFormat(factoryDW, "Arial", FontWeight.Bold, FontStyle.Normal, 32)
            {
                WordWrapping = WordWrapping.NoWrap,
                ParagraphAlignment = ParagraphAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            var textFormat3 = new TextFormat(factoryDW, "Arial", FontWeight.Normal, FontStyle.Normal, 48)
            {
                WordWrapping = WordWrapping.NoWrap,
                ParagraphAlignment = ParagraphAlignment.Far,
                TextAlignment = TextAlignment.Leading,
            };
            var textFormat4 = new TextFormat(factoryDW, "Arial", FontWeight.Bold, FontStyle.Normal, 48)
            {
                WordWrapping = WordWrapping.NoWrap,
                ParagraphAlignment = ParagraphAlignment.Far,
                TextAlignment = TextAlignment.Center,
            };

            // colors
            var backColor = new Color(0.15f, 0.15f, 0.15f, 1);

            // brushes
            var blackBrush = new SolidColorBrush(m_RenderTarget, Color.Black);
            var whiteBrush = new SolidColorBrush(m_RenderTarget, Color.White);
            var grayBrush = new SolidColorBrush(m_RenderTarget, Color.Gray);
            var lightGrayBrush = new SolidColorBrush(m_RenderTarget, Color.LightGray);
            var limeBrush = new SolidColorBrush(m_RenderTarget, Color.Lime);
            var goldBrush = new SolidColorBrush(m_RenderTarget, Color.Gold);
            var redBrush = new SolidColorBrush(m_RenderTarget, Color.Red);
            var enterWorldBrush = new SolidColorBrush(m_RenderTarget, Color.Orange);
            var playerJoinBrush = new SolidColorBrush(m_RenderTarget, Color.Cyan);
            var playerLeftBrush = new SolidColorBrush(m_RenderTarget, Color.HotPink);
            var playerLoginBrush = new SolidColorBrush(m_RenderTarget, Color.Yellow);
            var playerLogoutBrush = new SolidColorBrush(m_RenderTarget, Color.DeepPink);
            var playerGPSBrush = new SolidColorBrush(m_RenderTarget, Color.Lime);
            var group1Brush = new SolidColorBrush(m_RenderTarget, new Color(1, 0, 0.5f, 0.25f));
            var group2Brush = new SolidColorBrush(m_RenderTarget, new Color(0.5f, 1, 0, 0.25f));
            var group3Brush = new SolidColorBrush(m_RenderTarget, new Color(0, 0.5f, 1, 0.25f));

            // brushes (RenderTarget2)
            var blackBrush2 = new SolidColorBrush(m_RenderTarget2, Color.Black);
            var whiteBrush2 = new SolidColorBrush(m_RenderTarget2, Color.White);

            // images
            var sex = LoadBitmap(m_RenderTarget, Properties.Resources.icon_17);
            var controllerStatusOff = LoadBitmap(m_RenderTarget, Properties.Resources.controller_status_off);
            var controllerStatusReady = LoadBitmap(m_RenderTarget, Properties.Resources.controller_status_ready);
            var controllerStatusReadyLow = LoadBitmap(m_RenderTarget, Properties.Resources.controller_status_ready_low);
            var trackerStatusOff = LoadBitmap(m_RenderTarget, Properties.Resources.tracker_status_off);
            var trackerStatusReady = LoadBitmap(m_RenderTarget, Properties.Resources.tracker_status_ready);
            var trackerStatusReadyLow = LoadBitmap(m_RenderTarget, Properties.Resources.tracker_status_ready_low);
            var otherStatusOff = LoadBitmap(m_RenderTarget, Properties.Resources.other_status_off);
            var otherStatusReady = LoadBitmap(m_RenderTarget, Properties.Resources.other_status_ready);
            var otherStatusReadyLow = LoadBitmap(m_RenderTarget, Properties.Resources.other_status_ready_low);
            var leftControllerOff = LoadBitmap(m_RenderTarget, Properties.Resources.cb_left_controller_off);
            var leftControllerReady = LoadBitmap(m_RenderTarget, Properties.Resources.cb_left_controller_ready);
            var leftControllerReadyLow = LoadBitmap(m_RenderTarget, Properties.Resources.cb_left_controller_ready_low);
            var rightControllerOff = LoadBitmap(m_RenderTarget, Properties.Resources.cb_right_controller_off);
            var rightControllerReady = LoadBitmap(m_RenderTarget, Properties.Resources.cb_right_controller_ready);
            var rightControllerReadyLow = LoadBitmap(m_RenderTarget, Properties.Resources.cb_right_controller_ready_low);

            var epoch = DateTime.Now;
            var next = DateTime.MinValue;
            var uptime = string.Empty;

            RenderLoop.Run(m_RenderForm, () =>
            {
                if (DateTime.Now.CompareTo(next) >= 0)
                {
                    var sec = (DateTime.Now.Ticks - epoch.Ticks) / 10000000;
                    var min = sec / 60;
                    var hour = min / 60;
                    uptime = string.Format("Uptime {0:D2}:{1:D2}:{2:D2}", hour % 100, min % 60, sec % 60);
                    for (var i = m_NotifyInfos.Count - 1; i >= 0; --i)
                    {
                        if (DateTime.Now.CompareTo(m_NotifyInfos[i].Expire) >= 0)
                        {
                            m_NotifyInfos.RemoveAt(i);
                        }
                    }
                    next = DateTime.Now.AddSeconds(1);
                }

                Discord.Update();
                UpdateVR();

                var index = 0;

                // VR Overlay
                m_RenderTarget2.BeginDraw();
                m_RenderTarget2.Clear(null);
                for (var i = Math.Max(0, m_NotifyInfos.Count - 10); i < m_NotifyInfos.Count; ++i)
                {
                    var info = m_NotifyInfos[i];
                    m_RenderTarget2.FillRectangle(new RawRectangleF(0, 80 * i, 1024, 80 * i + 80), blackBrush2);
                    m_RenderTarget2.DrawText(info.Text, textFormat4, new RawRectangleF(0, 80 * i, 1024, 80 * i + 65), whiteBrush2, DrawTextOptions.None);
                }
                m_RenderTarget2.EndDraw();

                // Ingame
                m_RenderTarget.BeginDraw();
                m_RenderTarget.Clear(backColor);
                m_RenderTarget.DrawText(DateTime.Now.ToString("yyyy/MM/dd (ddd) tt hh:mm:ss"), textFormat1, new RawRectangleF(0, 0, XS, 80), whiteBrush, DrawTextOptions.Clip);
                {
                    var x = 5;
                    var y = YS - 60;
                    var cpu = PerformanceMonitor.CpuUsage;
                    m_RenderTarget.DrawText("CPU", textFormat3, new RawRectangleF(x, y + 50, x, y + 50), whiteBrush, DrawTextOptions.None);
                    m_RenderTarget.FillRectangle(new RawRectangleF(x + 115, y, x + 115 + 150, y + 48), whiteBrush);
                    m_RenderTarget.FillRectangle(new RawRectangleF(x + 115, y, x + 115 + 150 * cpu / 100, y + 48), (cpu >= 80f) ? redBrush : (cpu >= 50f) ? goldBrush : limeBrush);
                    m_RenderTarget.DrawText(cpu.ToString("N0") + "%", textFormat2, new RawRectangleF(x + 115, y, x + 115 + 150, y + 48), blackBrush, DrawTextOptions.Clip);
                    //
                    m_RenderTarget.DrawText(uptime, textFormat3, new RawRectangleF(660, YS - 10, 660, YS - 10), whiteBrush, DrawTextOptions.None);
                    m_RenderTarget.DrawText("InRoom:" + VRChatLog.InRoom, textFormat3, new RawRectangleF(300, YS - 10, 300, YS - 10), whiteBrush, DrawTextOptions.None);
                }
                index = 0;
                for (var i = Math.Max(0, m_ActivityInfos.Count - 21); i < m_ActivityInfos.Count; ++i)
                {
                    var info = m_ActivityInfos[i];
                    var brush = grayBrush;
                    if (info.Type == ActivityType.EnterWorld)
                    {
                        brush = enterWorldBrush;
                    }
                    else if (info.Type == ActivityType.Moderation)
                    {
                        brush = blackBrush;
                        m_RenderTarget.FillRectangle(new RawRectangleF(0, 300 + 55 * index, XS, 300 + 55 * index + 55), whiteBrush);
                    }
                    else if (info.Group >= 0)
                    {
                        switch (info.Type)
                        {
                            case ActivityType.PlayerJoined:
                                brush = playerJoinBrush;
                                break;
                            case ActivityType.PlayerLeft:
                                brush = playerLeftBrush;
                                break;
                            case ActivityType.PlayerLogin:
                                brush = playerLoginBrush;
                                break;
                            case ActivityType.PlayerLogout:
                                brush = playerLogoutBrush;
                                break;
                            case ActivityType.PlayerGPS:
                                brush = playerGPSBrush;
                                break;
                        }
                        switch (info.Group)
                        {
                            case 1:
                                m_RenderTarget.FillRectangle(new RawRectangleF(0, 300 + 55 * index, XS, 300 + 55 * index + 55), group1Brush);
                                break;
                            case 2:
                                m_RenderTarget.FillRectangle(new RawRectangleF(0, 300 + 55 * index, XS, 300 + 55 * index + 55), group2Brush);
                                break;
                            case 3:
                                m_RenderTarget.FillRectangle(new RawRectangleF(0, 300 + 55 * index, XS, 300 + 55 * index + 55), group3Brush);
                                break;
                        }
                    }
                    else if (info.Type == ActivityType.PlayerJoined)
                    {
                        brush = lightGrayBrush;
                    }
                    m_RenderTarget.DrawText(info.Text, textFormat3, new RawRectangleF(5, 300 + 55 * index, XS - 5, 300 + 55 * index + 55), brush, DrawTextOptions.Clip);
                    ++index;
                }
                index = 0;
                foreach (var info in m_DeviceInfos)
                {
                    var x = 12 + (index % 6) * (150 + 20);
                    var y = 100 + (index / 6) * (180 + 20);
                    switch (info.Class)
                    {
                        case ETrackedDeviceClass.Controller:
                            if (info.Oculus)
                            {
                                if (info.Role == ETrackedControllerRole.LeftHand)
                                {
                                    DrawBitmap(m_RenderTarget, info.Connected ? info.BatteryPercentage >= 0.2f ? leftControllerReady : leftControllerReadyLow : leftControllerOff, x + 11, y);
                                    break;
                                }
                                if (info.Role == ETrackedControllerRole.RightHand)
                                {
                                    DrawBitmap(m_RenderTarget, info.Connected ? info.BatteryPercentage >= 0.2f ? rightControllerReady : rightControllerReadyLow : rightControllerOff, x + 11, y);
                                    break;
                                }
                            }
                            DrawBitmap(m_RenderTarget, info.Connected ? info.BatteryPercentage >= 0.2f ? controllerStatusReady : controllerStatusReadyLow : controllerStatusOff, x + 11, y);
                            break;
                        case ETrackedDeviceClass.GenericTracker:
                            DrawBitmap(m_RenderTarget, info.Connected ? info.BatteryPercentage >= 0.2f ? trackerStatusReady : trackerStatusReadyLow : trackerStatusOff, x + 11, y);
                            break;
                        default:
                            DrawBitmap(m_RenderTarget, info.Connected ? info.BatteryPercentage >= 0.2f ? otherStatusReady : otherStatusReadyLow : otherStatusOff, x + 11, y);
                            break;
                    }
                    switch (info.Role)
                    {
                        case ETrackedControllerRole.LeftHand:
                            m_RenderTarget.DrawText("L", textFormat2, new RawRectangleF(x, y + 10, x + 75, y + 10 + 48), whiteBrush, DrawTextOptions.Clip);
                            break;
                        case ETrackedControllerRole.RightHand:
                            m_RenderTarget.DrawText("R", textFormat2, new RawRectangleF(x, y + 10, x + 75, y + 10 + 48), whiteBrush, DrawTextOptions.Clip);
                            break;
                    }
                    y += 128 + 10;
                    m_RenderTarget.FillRectangle(new RawRectangleF(x, y, x + 150, y + 48), whiteBrush);
                    m_RenderTarget.FillRectangle(new RawRectangleF(x, y, x + 150 * info.BatteryPercentage, y + 48), (info.BatteryPercentage >= 0.5f) ? limeBrush : (info.BatteryPercentage >= 0.2f) ? goldBrush : redBrush);
                    m_RenderTarget.DrawText((info.BatteryPercentage * 100f).ToString("N0") + "%", textFormat2, new RawRectangleF(x, y, x + 150, y + 48), blackBrush, DrawTextOptions.Clip);
                    ++index;
                }
                if (index == 0)
                {
                    m_RenderTarget.DrawText("No SteamVR Devices", textFormat2, new RawRectangleF(5, 100, XS - 5, 300), whiteBrush, DrawTextOptions.Clip);
                }
                m_RenderTarget.EndDraw();
                m_SwapChain.Present(1, PresentFlags.None);
                RenderVR();
            });

            VRCApi.SaveCookie();
            LocalConfig.SaveConfig();
            Discord.Stop();
            PerformanceMonitor.Stop();
        }
    }
}