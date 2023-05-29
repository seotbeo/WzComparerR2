using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WzComparerR2.Avatar.UI
{
    public partial class EffectForm : Form
    {
        public AvatarForm Af1;
        public int recentitem = 0;
        public Dictionary<int, string> itemDescDic = new Dictionary<int, string>();
        public List<int[]> effectDelayList = new List<int[]>();
        public List<bool> effectAnimateBool = new List<bool>();
        public Dictionary<int, EffectLayer> EffectLayerGroup = new Dictionary<int, EffectLayer>();
        public bool ListBoxChanging = false;
        public EffectForm(AvatarForm AvF)
        {
            InitializeComponent();
            Af1 = AvF;
        }
        public struct EffectLayer
        {
            public int itemcode;//아이템 코드
            public int[] delays;//프레임의 딜레이
            public int currentFrame; //현재 프레임 위치
            public bool animated; //애니메이션 보기/안보기

        }
        struct EffectActionFrame
        {
            int itemcode;
            string Action;
            int FrameNum;

        }

        public void AnimateAllStop()
        {
            EffectLayer changeLayer;
            List<int> itemList = new List<int>();
            foreach (var item in ItemEffectListBox1.Items)
            {
                try
                {
                    itemList.Add(Convert.ToInt32(item));
                }
                catch
                {
                }
            }
            foreach (var itemcode in itemList)
            {
                changeLayer = EffectLayerGroup[itemcode];
                changeLayer.animated = false;
                EffectLayerGroup[itemcode] = changeLayer;
            }
            EffectMoveBox.Checked = false;

        }
        private void ItemEffectListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBoxChanging = true;
            int selectedItemCode = 0;
            selectedItemCode = Convert.ToInt32(ItemEffectListBox1.SelectedItem);

            recentitem = selectedItemCode;
            Af1.FillCmbEffect(selectedItemCode);
            EffectComboBox.SelectedItem = EffectLayerGroup[selectedItemCode].currentFrame;
            EffectMoveBox.Checked = EffectLayerGroup[selectedItemCode].animated;
            ListBoxChanging = false;
        }

        private void EffectComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ListBoxChanging)
            {
                EffectLayer changeLayer;
                int selectedFrameNumber = 0;
                int selectedItemCode = 0;
                selectedItemCode = Convert.ToInt32(ItemEffectListBox1.SelectedItem);
                selectedFrameNumber = Convert.ToInt32(EffectComboBox.SelectedItem);
                /*
                foreach (var el in EffectLayerGroup)
                {
                    if(el.itemcode == selectedItemCode)
                    {
                        el.currentFrame = selectedFrameNumber;
                    }
                }
                */
                //Dic
                changeLayer = EffectLayerGroup[selectedItemCode];
                changeLayer.currentFrame = selectedFrameNumber;
                EffectLayerGroup[selectedItemCode] = changeLayer;
                Af1.ChangeEffectStruct(selectedItemCode, selectedFrameNumber);
                Af1.UpdateDisplay();
            }
        }
        public string ToStringEffectList(List<EffectStruction> effectStructs)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var efs in effectStructs)
            {
                sb.Append(efs.ToString());

            }
            return sb.ToString();
        }


        private void Button1_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            int haircode = Convert.ToInt32(MixHairComboBox.SelectedValue);
            int colorcode = haircode % 10;
            int opacityvalue = 0;
            if (int.TryParse(MixHairOpacityText.Text, out opacityvalue))
            {
                if ((opacityvalue >= 0) && (opacityvalue <= 100))
                {
                    Af1.ChangeMixHair(colorcode, opacityvalue);
                }

            }

        }

        private void EffectMoveBox_CheckedChanged(object sender, EventArgs e)
        {
            if (!ListBoxChanging)
            {
                if (ItemEffectListBox1.SelectedItem != null)
                {
                    if (EffectMoveBox.Checked)
                    {
                        EffectLayer changeLayer;
                        int selectedItemCode = Convert.ToInt32(ItemEffectListBox1.SelectedItem);
                        changeLayer = EffectLayerGroup[selectedItemCode];
                        changeLayer.animated = true;
                        EffectLayerGroup[selectedItemCode] = changeLayer;
                        if (!Af1.effectTimer.Enabled)
                        {
                            Af1.EffectAnimateStart();
                        }


                        if (EffectLayerGroup.ContainsKey(selectedItemCode) && (EffectLayerGroup[selectedItemCode].delays != null))
                        {
                            if (EffectLayerGroup[selectedItemCode].delays[EffectLayerGroup[selectedItemCode].currentFrame] >= 0)
                            {
                                Af1.setEffectDelay(selectedItemCode);
                            }

                        }
                    }
                    else
                    {
                        EffectLayer changeLayer;
                        int selectedItemCode = Convert.ToInt32(ItemEffectListBox1.SelectedItem);
                        changeLayer = EffectLayerGroup[selectedItemCode];
                        changeLayer.animated = false;
                        EffectLayerGroup[selectedItemCode] = changeLayer;
                        if (EffectLayerGroup.ContainsKey(selectedItemCode) && (EffectLayerGroup[selectedItemCode].delays != null))
                        {
                            if (EffectLayerGroup[selectedItemCode].delays[EffectLayerGroup[selectedItemCode].currentFrame] >= 0)
                            {
                                Af1.resetEffectDelay(selectedItemCode);
                            }
                        }
                        Af1.EffectTimerEnabledCheck();
                    }
                }
            }
        }
    }
}