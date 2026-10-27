//Copyright (c) 2016-2021 Diego Settimi - https://github.com/arkypita/

// This program is free software; you can redistribute it and/or modify  it under the terms of the GPLv3 General Public License as published by  the Free Software Foundation; either version 3 of the License, or (at  your option) any later version.
// This program is distributed in the hope that it will be useful, but  WITHOUT ANY WARRANTY; without even the implied warranty of  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GPLv3  General Public License for more details.
// You should have received a copy of the GPLv3 General Public License  along with this program; if not, write to the Free Software  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307,  USA. using System;

using System;
using System.Collections.Generic;
using System.Text;

namespace LaserGRBL
{
	[Serializable()]
	public class CustomButtons
	{
		private static List<CustomButton> buttons;

		private static string UserFile { get => System.IO.Path.Combine(GrblCore.DataPath, "CustomButtons.bin"); }
		private static string StandardFile { get => System.IO.Path.Combine(LaserGRBL.GrblCore.ExePath, "StandardButtons.zbn"); }

		public static void LoadFile() //in ingresso
		{
			if (!System.IO.File.Exists(UserFile) && System.IO.File.Exists(StandardFile))
			{
				try { System.IO.File.Copy(StandardFile, UserFile, false); }
				catch { }
			}

			buttons = (List<CustomButton>)Tools.Serializer.ObjFromFile(UserFile);
			if (buttons == null)
			{
				if (buttons == null) buttons = (List<CustomButton>)Settings.GetAndDeleteObject("Custom Buttons", null);
				if (buttons == null) buttons = new List<CustomButton>();
				SaveFile();
			}

			EnsureFocusButton();
			EnsureZeroButtons();
			EnsureFrameButton();
			EnsureBedButton();
			EnsureKeychainButton();
		}

		public static void EnsureFocusButton()
		{
			if (buttons == null)
				buttons = new List<CustomButton>();

			CustomButton focusBtn = null;
			foreach (CustomButton b in buttons)
			{
				if (b != null && (b.Caption == "Focus" || (b.Caption != "Frame" && b.GCode != null && b.GCode.Contains("G1 F1") && (b.GCode.Contains("S10") || b.GCode.Contains("S30")))))
				{
					focusBtn = b;
					break;
				}
			}

			if (focusBtn == null)
			{
				focusBtn = new CustomButton()
				{
					Caption = "Focus",
					ToolTip = "Laser 3% (Foco / Mira)",
					GCode = "$X\r\nG1 F1\r\nM3 S30",
					GCode2 = "M5 S0\r\nG0",
					ButtonType = CustomButton.ButtonTypes.TwoStateButton,
					EnableStyle = CustomButton.EnableStyles.Connected
				};
				buttons.Add(focusBtn);
				SaveFile();
			}
			else
			{
				bool needsUpdate = false;
				if (focusBtn.Caption != "Focus" || focusBtn.GCode != "$X\r\nG1 F1\r\nM3 S30" || focusBtn.GCode2 != "M5 S0\r\nG0" || focusBtn.ToolTip != "Laser 3% (Foco / Mira)")
				{
					focusBtn.Caption = "Focus";
					focusBtn.GCode = "$X\r\nG1 F1\r\nM3 S30";
					focusBtn.GCode2 = "M5 S0\r\nG0";
					focusBtn.ToolTip = "Laser 3% (Foco / Mira)";
					focusBtn.ButtonType = CustomButton.ButtonTypes.TwoStateButton;
					focusBtn.EnableStyle = CustomButton.EnableStyles.Connected;
					needsUpdate = true;
				}
				if (needsUpdate)
				{
					SaveFile();
				}
			}
		}

		public static void EnsureZeroButtons()
		{
			if (buttons == null)
				buttons = new List<CustomButton>();

			bool hasZero = false;
			bool hasGoZero = false;
			foreach (CustomButton b in buttons)
			{
				if (b != null)
				{
					if (b.Caption == "Zero" || (b.GCode != null && b.GCode.Contains("G92") && b.GCode.Contains("X0") && b.GCode.Contains("Y0")))
						hasZero = true;
					if (b.Caption == "GoZero" || (b.GCode != null && b.GCode.Contains("G0") && b.GCode.Contains("X0") && b.GCode.Contains("Y0")))
						hasGoZero = true;
				}
			}

			bool changed = false;

			if (!hasZero)
			{
				CustomButton zeroBtn = new CustomButton()
				{
					Caption = "Zero",
					ToolTip = "Definir Ponto Zero (0,0) na posição atual [G92 X0 Y0]",
					GCode = "$X\r\nG92 X0 Y0",
					ButtonType = CustomButton.ButtonTypes.Button,
					EnableStyle = CustomButton.EnableStyles.Connected
				};
				buttons.Add(zeroBtn);
				changed = true;
			}

			if (!hasGoZero)
			{
				CustomButton goZeroBtn = new CustomButton()
				{
					Caption = "GoZero",
					ToolTip = "Voltar para o Ponto Zero inicial (0,0) [G0 X0 Y0]",
					GCode = "$X\r\nG90\r\nG0 X0 Y0",
					ButtonType = CustomButton.ButtonTypes.Button,
					EnableStyle = CustomButton.EnableStyles.Connected
				};
				buttons.Add(goZeroBtn);
				changed = true;
			}

			if (changed)
			{
				SaveFile();
			}
		}

		public static void EnsureFrameButton()
		{
			if (buttons == null)
				buttons = new List<CustomButton>();

			bool hasFrame = false;
			foreach (CustomButton b in buttons)
			{
				if (b != null && (b.Caption == "Frame" || (b.GCode != null && b.GCode.Contains("[left]") && b.GCode.Contains("[right]"))))
				{
					hasFrame = true;
					break;
				}
			}

			if (!hasFrame)
			{
				CustomButton frameBtn = new CustomButton()
				{
					Caption = "Frame",
					ToolTip = "Contorno da Área de Gravação (Framing com Mira 3%)",
					GCode = "$X\r\nG90\r\nG0 X[left] Y[bottom]\r\nG1 F1000\r\nM3 S30\r\nG1 Y[top]\r\nG1 X[right]\r\nG1 Y[bottom]\r\nG1 X[left]\r\nM5 S0\r\nG0 X0 Y0",
					ButtonType = CustomButton.ButtonTypes.Button,
					EnableStyle = CustomButton.EnableStyles.IdleProgram
				};
				buttons.Add(frameBtn);
				SaveFile();
			}
		}

		public static void EnsureBedButton()
		{
			if (buttons == null)
				buttons = new List<CustomButton>();

			bool hasBed = false;
			foreach (CustomButton b in buttons)
			{
				if (b != null && (b.Caption == "Bed300" || (b.GCode != null && b.GCode.Contains("$130=300") && b.GCode.Contains("$131=300"))))
				{
					hasBed = true;
					break;
				}
			}

			if (!hasBed)
			{
				CustomButton bedBtn = new CustomButton()
				{
					Caption = "Bed300",
					ToolTip = "Configurar Área da Mesa 300x300mm (Two Trees) e Ativar Limites",
					GCode = "$130=300\r\n$131=300",
					ButtonType = CustomButton.ButtonTypes.Button,
					EnableStyle = CustomButton.EnableStyles.Connected
				};
				buttons.Add(bedBtn);
				SaveFile();
			}
		}

		public static void EnsureKeychainButton()
		{
			if (buttons == null)
				buttons = new List<CustomButton>();

			bool hasBtn = false;
			foreach (CustomButton b in buttons)
			{
				if (b != null && (b.Caption == "Chaveiro" || b.Caption == "Ninho" || (b.GCode != null && b.GCode.Contains("Gabarito de Chaveiro"))))
				{
					hasBtn = true;
					break;
				}
			}

			if (!hasBtn)
			{
				CustomButton jigBtn = new CustomButton()
				{
					Caption = "Chaveiro",
					ToolTip = "Gravar Ninho no Papelão (15% @ 5000 mm/min) para Chaveiro Redondo",
					GCode = "; === Gabarito de Chaveiro (Ninho no Papelão 15% @ 5000) ===\r\nG90\r\nG21\r\nM5 S0\r\nG0 X30 Y50\r\nM3 S150\r\nG2 X30 Y50 I20 J0 F5000\r\nM5 S0\r\nG0 X0 Y0",
					ButtonType = CustomButton.ButtonTypes.Button,
					EnableStyle = CustomButton.EnableStyles.Connected
				};
				buttons.Add(jigBtn);
				SaveFile();
			}
		}

		public static void SaveFile()
		{
			Tools.Serializer.ObjToFile(buttons, UserFile); //salva
		}

		internal static void Reorder(int oldindex, int newindex)
		{
			CustomButton item = buttons[oldindex];
			buttons.RemoveAt(oldindex);
			if(oldindex < newindex && newindex > 0)
			{
				newindex--; // removing the element from the list, has impact on the index
			}
			if (newindex < 0 || newindex > buttons.Count)
				buttons.Add(item);
			else
				buttons.Insert(newindex, item);

			SaveFile();
		}

		internal static void Add(CustomButton cb)
		{buttons.Add(cb);}

		public static IEnumerable<CustomButton> Buttons
		{ get { return buttons; } }

		internal static void Remove(CustomButton cb)
		{buttons.Remove(cb);}

		internal static CustomButton GetButton(int index)
		{
			if (buttons != null && buttons.Count > index)
				return buttons[index];
			else
				return null;
		}

		public static void Export(System.Windows.Forms.Form parent)
		{
			using (System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog())
			{
				sfd.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				sfd.Filter = "ZippedButton|*.zbn";
				sfd.AddExtension = true;
				sfd.FileName = "CustomButtons.zbn";

				System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.DialogResult.Cancel;
				try
				{
					dialogResult = sfd.ShowDialog(parent);
				}
				catch (System.Runtime.InteropServices.COMException)
				{
					sfd.AutoUpgradeEnabled = false;
					dialogResult = sfd.ShowDialog(parent);
				}

				if (dialogResult == System.Windows.Forms.DialogResult.OK && sfd.FileName != null)
					Tools.Serializer.ObjToFile(buttons, sfd.FileName, Tools.Serializer.SerializationMode.Binary, null, true);
			}
		}

		public static bool Import(System.Windows.Forms.Form parent, string filename = null)
		{
			if (filename == null)
			{
				using (System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog())
				{
					ofd.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
					ofd.Filter = "ZippedButton|*.zbn;*.gz";
					ofd.AddExtension = true;

					System.Windows.Forms.DialogResult dialogResult = System.Windows.Forms.DialogResult.Cancel;
					try
					{
						dialogResult = ofd.ShowDialog(parent);
					}
					catch (System.Runtime.InteropServices.COMException)
					{
						ofd.AutoUpgradeEnabled = false;
						dialogResult = ofd.ShowDialog(parent);
					}

					if (dialogResult == System.Windows.Forms.DialogResult.OK && ofd.FileName != null)
						filename = ofd.FileName;
				}
			}

			if (filename != null && System.IO.File.Exists(filename))
			{
				try
				{
					List<CustomButton> list = Tools.Serializer.ObjFromFile(filename) as List<CustomButton>;
					if (list.Count > 0)
					{
						System.Windows.Forms.DialogResult rv = buttons.Count == 0 ? System.Windows.Forms.DialogResult.No : System.Windows.Forms.MessageBox.Show(Strings.BoxImportCustomButtonClearText, Strings.BoxImportCustomButtonCaption, System.Windows.Forms.MessageBoxButtons.YesNoCancel);

						if (rv == System.Windows.Forms.DialogResult.Yes || rv == System.Windows.Forms.DialogResult.No)
						{
							if (rv == System.Windows.Forms.DialogResult.Yes)
								buttons.Clear();

							foreach (CustomButton cb in list)
							{
								if (System.Windows.Forms.MessageBox.Show(string.Format("Import \"{0}\"?", cb.ToolTip.Trim().Length > 0 ? cb.ToolTip : "[no name]"), Strings.BoxImportCustomButtonCaption, System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
									buttons.Add(cb);
							}

							return true;
						}
					}
				}
				catch { System.Windows.Forms.MessageBox.Show($"Error importing custom buttons from {filename}"); }
			}

			return false;
		}

		public static int Count { get { return buttons.Count; } }
	}


	[Serializable]
	public class CustomButton
	{
		public enum EnableStyles { Always = 0, Connected = 1, Idle = 3, Run = 4, IdleProgram = 10}
		public enum ButtonTypes { Button = 0, TwoStateButton = 1, PushButton =2 }

		public System.Guid guid = Guid.NewGuid();
		public System.Drawing.Image Image;
		public string GCode;
		public string GCode2;
		public string Caption;
		public string ToolTip;

		public EnableStyles EnableStyle;
		public ButtonTypes ButtonType;

		public bool EnabledNow(GrblCore core)
		{
			if (EnableStyle == EnableStyles.Always)
				return true;
			else if (EnableStyle == EnableStyles.Connected)
				return core.IsConnected;
			else if (EnableStyle == EnableStyles.Idle)
				return core.MachineStatus == GrblCore.MacStatus.Idle;
			else if (EnableStyle == EnableStyles.Run)
				return core.MachineStatus == GrblCore.MacStatus.Run;
			else if (EnableStyle == EnableStyles.IdleProgram)
				return core.MachineStatus == GrblCore.MacStatus.Idle && core.HasProgram;

			return false;
		}
	}
}
