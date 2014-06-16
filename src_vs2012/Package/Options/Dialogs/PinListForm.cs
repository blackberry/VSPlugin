using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using RIM.VSNDK_Package.ViewModels;

namespace RIM.VSNDK_Package.Options.Dialogs
{
    /// <summary>
    /// Dialog for creating list of device PINs.
    /// </summary>
    internal partial class PinListForm : Form
    {
        private List<ulong> _pins;

        public PinListForm()
        {
            InitializeComponent();

            _pins = new List<ulong>();
        }

        #region Properties

        public string NewPIN
        {
            get { return txtPIN.Text; }
            set { txtPIN.Text = value; }
        }

        public ulong[] PINs
        {
            get { return _pins.ToArray(); }
            set
            {
                if (value == null)
                    _pins = new List<ulong>();
                else
                    _pins = new List<ulong>(value);

                PopulateList();
            }
        }

        internal IEnumerable<DeviceDefinition> OptionalDevices
        {
            get;
            set;
        }

        #endregion

        private void PopulateList()
        {
            listPINs.Items.Clear();

            for (int i = 0; i < _pins.Count; i++)
                listPINs.Items.Add(_pins[i].ToString("X"));

            bttClear.Enabled = _pins.Count > 0;
        }

        /// <summary>
        /// Adds new PIN to the collection.
        /// </summary>
        public int Add(ulong pin)
        {
            if (_pins.IndexOf(pin) < 0)
            {
                _pins.Insert(0, pin);

                PopulateList();
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Adds a list of PINs to the collection.
        /// </summary>
        public int Add(IEnumerable<ulong> pins)
        {
            if (pins == null)
                return 0;

            int count = 0;
            foreach (var p in pins)
            {
                if (_pins.IndexOf(p) < 0)
                {
                    _pins.Insert(count, p);
                    count++;
                }
            }

            PopulateList();
            return count;
        }

        /// <summary>
        /// Adds new PINs read from specified string (can be a comma-separated list of values).
        /// </summary>
        public int Add(string pin)
        {
            if (string.IsNullOrEmpty(pin))
                return 0;

            var items = pin.Split(new[] { ' ', ',', ';', '|', '#', '*', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var pins = new ulong[items.Length];

            for(int i = 0; i < items.Length; i++)
            {
                ulong value;

                // try to parse all or NONE!
                if (!ulong.TryParse(items[i], NumberStyles.HexNumber, null, out value))
                    return -1;

                pins[i] = value;
            }

            // add all to the existing list:
            _pins.AddRange(pins);
            PopulateList();
            return pins.Length;
        }

        private void bttAdd_Click(object sender, EventArgs e)
        {
            if (Add(NewPIN) < 0)
            {
                MessageBoxHelper.Show("PIN number contains errors. Please correct them and try again", "Invalid PIN value", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            NewPIN = string.Empty;
        }

        private void bttClear_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelper.Show("Clear the list?", "PIN List", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _pins.Clear();
                PopulateList();
            }
        }

        private void listPINs_SelectedIndexChanged(object sender, EventArgs e)
        {
            bttRemove.Enabled = listPINs.SelectedItems.Count > 0;
        }

        private void bttRemove_Click(object sender, EventArgs e)
        {
            if (MessageBoxHelper.Show("Remove this item?", _pins[listPINs.SelectedIndices[0]].ToString("X"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _pins.RemoveAt(listPINs.SelectedIndices[0]);
                PopulateList();
            }
        }

        private void bttDiscover_Click(object sender, EventArgs e)
        {
            var form = new DeviceForm("Discover Device PIN");
            form.SetDiscoverMode(OptionalDevices);

            if (form.ShowDialog() == DialogResult.OK && form.PIN != 0)
            {
                Add(form.PIN);
            }

            ActiveControl = txtPIN;
        }
    }
}
