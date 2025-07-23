using C1.Win.C1List;
using Dasof.Processing;
using Microsoft.Office.Interop.Excel;
using Rok.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Dynamic;
using System.Security.AccessControl;
using System.Text;
using System.Windows.Forms;

namespace Dasof.Common.Klienti.PO_RO
{
    /*
    since you're creating a control, any events that you want to expose should be on the control's level, not on the level of one of its constituant controls. 
    In your case that means you should create a public event, eg TestButton_Click. In the constructor, you could hook up the buttons click event with a private eventhandler, and in that eventhandler, you could raise your TestButton_Click event.
    
    The problem is in the fact that the button you are trying to create an event handler for is not in design mode, i.e. does not have a site associated with it, as the error states.
    In order to make it sited you need to call EnableDesignMode for that button from the composite control's custom designer. Which means that you have to create a custom designer for your user control and call the above method for the button. The user would then be able to click directly on the button and set its properties in design time, or hook up an event handler for any event. 

    //VIR: https://social.msdn.microsoft.com/Forums/windows/en-US/6b963d02-1676-4b43-b87c-6a2dbf0bda1e/adding-events-on-properties-of-a-user-control-in-designer-maybe-a-bug?forum=winformsdesigner
    */

    [MultiLanguage.Translate(Include = new[] { "PO_RO" })]
    public partial class PostaEditor : UserControlEx
    {
        #region Variables

        private bool init;

        private string country;
        private string county;
        private string sector;
        private string city;
        private string type;
        private string street;
        private string number;
        private string post;

        private bool blocked;
        private bool blockedInit;

        //private ComboBoxChainManager<object> _chainManager;

        #endregion

        #region Constructor

        public PostaEditor()
        {
            InitializeComponent();
            if (MultiLanguage.Translator.InTranslationMode())
                return;
            if (GLOBALS.Translator != null)
                GLOBALS.Translator.TranslateControl(this);

            CopyAddressButtonShow = false;

            ToolTip1.SetToolTip(btnCopyAddress, "Copy Address to Postal address");
#if DEBUG
            this.coCountry.SelectedIndexChanged += this.PostalCodes_SelectedIndexChangedcoCountry;
            this.coCounty.SelectedIndexChanged += this.PostalCodes_SelectedIndexChangedcoCounty;
            this.coCity.SelectedIndexChanged += this.PostalCodes_SelectedIndexChangedcoCity;
            this.coSector.SelectedIndexChanged += this.PostalCodes_SelectedIndexChangedcoSector;
            this.coStreetType.SelectedIndexChanged += this.PostalCodes_SelectedIndexChangedcoStreetType;
            this.coStreet.SelectedIndexChanged += this.PostalCodes_SelectedIndexChangedcoStreet;
            this.coHouseNumber.SelectedIndexChanged += this.PostalCodes_SelectedIndexChangedcoHouseNumber;
#endif
        }

        #endregion

        private void PostaEditor_Load(object sender, EventArgs e) // pride iz: PopupHelper.Show .. in iz UserControlCustomEditorNotTabPage.Dispose -> pnlButtons?.Dispose()
        {
#if DEBUG
            var f = this.FindForm();
            var b1 = (f?.Disposing ?? false) || (f?.IsDisposed ?? false);

            var p = this.Parent?.Parent;
            var b2 = (p?.Disposing ?? false) || (p?.IsDisposed ?? false);

            if (this.Name == "postaEditor1")
            {
                var dummy = 1;
            }

            SetupComboBox(coCountry);
            SetupComboBox(coCounty);
            SetupComboBox(coCity);
            SetupComboBox(coSector);
            SetupComboBox(coStreetType);
            SetupComboBox(coStreet);
            SetupComboBox(coHouseNumber);
#endif
        }

        #region Properties

        #region CopyAddressButton

        //INFO(Winforms): how to allow a Control, which is a child of another Control to accept having controls dropped onto it at design time. VIR: https://www.codeproject.com/Articles/37830/Designing-Nested-Controls
        // A very simple solution rather than having custom events, would be to expose the nested control as a property of the custom control. //VIR: https://stackoverflow.com/questions/3310661/exposing-events-of-underlying-control

        private bool _CopyAddressButtonShow;

        [Browsable(true), DefaultValue(false)]
        public bool CopyAddressButtonShow
        {
            get => _CopyAddressButtonShow;
            set
            {
                _CopyAddressButtonShow = value;

                this.btnCopyAddress.Visible = value;
            }
        }

        [Browsable(true), DefaultValue(null)]
        public event EventHandler CopyAddressButtonAction //VIR: https://www.akadia.com/services/dotnet_user_controls.html
        {
            add => this.btnCopyAddress.Click += value;
            remove => this.btnCopyAddress.Click -= value;
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image CopyAddressImage => btnCopyAddress.Image;

        #endregion

        private bool IsRomania
        {
            get
            {
                if (coCountry.SelectedItem == null)
                    return false;

                return coCountry.SelectedValue.ToString() == "RO";
            }
        }

        #region Values - Country .. Others

#pragma warning disable S4275 // Getters and setters should access the expected fields VIR: https://rules.sonarsource.com/csharp/RSPEC-4275/

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Country
        {
            get { return coCountry.SelectedValue != null ? coCountry.SelectedValue.ToString() : String.Empty; }
            set
            {
                if (coCountry.Items.Count == 0)
                    NapolniCountry();
                country = value;
            }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string County
        {
            get { return coCounty.Text.Trim(); }
            set { county = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Sector
        {
            get { return coSector.Text.Trim(); }
            set { sector = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string City
        {
            get { return coCity.Text.Trim(); }
            set { city = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Type
        {
            get { return coStreetType.Text.Trim(); }
            set { type = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Street
        {
            get { return coStreet.Text.Trim(); }
            set { street = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Number
        {
            get { return tbHouseNumber.Text.Trim(); }
            set { number = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Post
        {
            get { return tbPostalCode.Text.Trim(); }
            set { post = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Building
        {
            get { return tbBuilding.Text.Trim(); }
            set { tbBuilding.Text = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Entrance
        {
            get { return tbEntrance.Text.Trim(); }
            set { tbEntrance.Text = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Flat
        {
            get { return tbFlat.Text.Trim(); }
            set { tbFlat.Text = value; }
        }

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Others
        {
            get { return tbOthers.Text.Trim(); }
            set { tbOthers.Text = value; }
        }

        private Item SelectedCounty
        {
            get { return coCounty.SelectedItem as Item; }
        }

        private Item SelectedCity
        {
            get { return coCity.SelectedItem as Item; }
        }

        private Item SelectedSector
        {
            get { return coSector.SelectedItem as Item; }
        }

        private Item SelectedStreetType
        {
            get { return coStreetType.SelectedItem as Item; }
        }

        private Item SelectedStreet
        {
            get { return coStreet.SelectedItem as Item; }
        }

        private RangeItem SelectedStreetNumber
        {
            get { return coHouseNumber.SelectedItem as RangeItem; }
        }

#pragma warning restore S4275 // Getters and setters should access the expected fields

        #endregion

        public string Naslov
        {
            get
            {
                var sb = new StringBuilder();

                if (!String.IsNullOrEmpty(City))
                    sb.Append(City);

                if (!String.IsNullOrEmpty(Type))
                {
                    sb.Append(GLOBALS.Settings.VmesUlicaStevilka.Replace("#", ""));
                    sb.Append(Type);
                }
                if (!String.IsNullOrEmpty(Street))
                {
                    sb.Append(GLOBALS.Settings.VmesUlicaStevilka.Replace("#", ""));
                    sb.Append(Street);
                }
                if (Number != String.Empty)
                {
                    sb.Append(GLOBALS.Settings.VmesUlicaStevilka.Replace("#", ""));
                    sb.Append(Number);
                }
                return sb.ToString();
            }
        }

        #endregion

        public void OnemogociVse()
        {
            btnCopyAddress.Enabled = false;
        }

        public void ClearData()
        {
            blockedInit = true;

            //coCountry.SelectedValue = null; //NE GRE -> Value cannot be null. Parameter name: key
            coCountry.SelectedIndex = -1;
            if (coCountry.SelectedIndex != -1)
                coCountry.SelectedIndex = -1; //TODO: zakaj je treba 2x ?!

            coCounty.SelectedIndex = -1;
            coCity.SelectedIndex = -1;
            coSector.SelectedIndex = -1;
            coStreetType.SelectedIndex = -1;
            coStreet.SelectedIndex = -1;

            tbHouseNumber.Text = string.Empty;
            coHouseNumber.SelectedIndex = -1;
            tbPostalCode.Text = string.Empty;
            tbBuilding.Text = string.Empty;
            tbEntrance.Text = string.Empty;
            tbFlat.Text = string.Empty;
            tbOthers.Text = string.Empty;

            country = string.Empty;
            county = string.Empty;
            sector = string.Empty;
            city = string.Empty;
            type = string.Empty;
            street = string.Empty;
            number = string.Empty;
            post = string.Empty;

            blockedInit = false;
        }

        private void PostalCodes_SelectedIndexChangedcoCountry(object sender, EventArgs e)
        {
            if (blocked || blockedInit) return;
            ComboBox combo = sender as ComboBox;
            var callerInfo = $"PostalCodes_SelectedIndexChanged({combo.Name}[{(combo.DataSource as BindingSource)?.Count ?? 0}].SelIdx={combo.SelectedIndex})";
            NapolniCounty(callerInfo);
        }
        private void PostalCodes_SelectedIndexChangedcoCounty(object sender, EventArgs e)
        {
            if (blocked || blockedInit) return;
            ComboBox combo = sender as ComboBox;
            var callerInfo = $"PostalCodes_SelectedIndexChanged({combo.Name}[{(combo.DataSource as BindingSource)?.Count ?? 0}].SelIdx={combo.SelectedIndex})";
            NapolniCity(callerInfo);
        }
        private void PostalCodes_SelectedIndexChangedcoCity(object sender, EventArgs e)
        {
            if (blocked || blockedInit) return;
            ComboBox combo = sender as ComboBox;
            var callerInfo = $"PostalCodes_SelectedIndexChanged({combo.Name}[{(combo.DataSource as BindingSource)?.Count ?? 0}].SelIdx={combo.SelectedIndex})";
            NapolniSector(callerInfo);
        }
        private void PostalCodes_SelectedIndexChangedcoSector(object sender, EventArgs e)
        {
            if (blocked || blockedInit) return;
            ComboBox combo = sender as ComboBox;
            var callerInfo = $"PostalCodes_SelectedIndexChanged({combo.Name}[{(combo.DataSource as BindingSource)?.Count ?? 0}].SelIdx={combo.SelectedIndex})";
            NapolniStreetType(callerInfo);
        }
        private void PostalCodes_SelectedIndexChangedcoStreetType(object sender, EventArgs e)
        {
            if (blocked || blockedInit) return;
            ComboBox combo = sender as ComboBox;
            var callerInfo = $"PostalCodes_SelectedIndexChanged({combo.Name}[{(combo.DataSource as BindingSource)?.Count ?? 0}].SelIdx={combo.SelectedIndex})";
            NapolniStreet(callerInfo);
        }
        private void PostalCodes_SelectedIndexChangedcoStreet(object sender, EventArgs e)
        {
            if (blocked || blockedInit) return;
            ComboBox combo = sender as ComboBox;
            var callerInfo = $"PostalCodes_SelectedIndexChanged({combo.Name}[{(combo.DataSource as BindingSource)?.Count ?? 0}].SelIdx={combo.SelectedIndex})";
            NapolniNumbers(callerInfo);
        }
        private void PostalCodes_SelectedIndexChangedcoHouseNumber(object sender, EventArgs e)
        {
            if (blocked || blockedInit) return;
            ComboBox combo = sender as ComboBox;
            var callerInfo = $"PostalCodes_SelectedIndexChanged({combo.Name}[{(combo.DataSource as BindingSource)?.Count ?? 0}].SelIdx={combo.SelectedIndex})";
            NapolniPost(callerInfo);
        }

        private void PostalCodes_SelectedIndexChanged(object sender, EventArgs e)
        {
#if DEBUG
            return;

            if ((sender as ComboBox).Name == "coCountry")
            {
                var dummy = 1;
            }
#endif
            //bool isnull = (sender as ComboBox)?.DataSource == null; //(((sender as ComboBox).DataSource as BindingSource)?.Count == 0 ?? false)
            if (blocked || blockedInit) //|| isnull
                return;

            ComboBox combo = sender as ComboBox;
            if (combo == null)
                return;

            string comboName = combo.Name;
            string callerInfo = string.Empty;
#if DEBUG
            if (comboName == "coCountry")
            {
                var dummy = 1;
            }
            callerInfo = $"PostalCodes_SelectedIndexChanged({comboName}[{(combo.DataSource as BindingSource)?.Count ?? 0}].SelIdx={combo.SelectedIndex})";
#endif
            switch (comboName)
            {
                case "coCountry": NapolniCounty(callerInfo); break;
                case "coCounty": NapolniCity(callerInfo); break;
                case "coCity": NapolniSector(callerInfo); break;
                case "coSector": NapolniStreetType(callerInfo); break;
                case "coStreetType": NapolniStreet(callerInfo); break;
                case "coStreet": NapolniNumbers(callerInfo); break;
                case "coHouseNumber": NapolniPost(callerInfo); break;
            }

            CheckInput(sender, e);
        }

        #region Napolni

        private void BlockOnIndexChanged(bool block)
        {
            blocked = block;
        }

        private static void SetSelectedIndex(ComboBox cmb, string value)
        {
            if (!string.IsNullOrEmpty(value))
                cmb.SelectedIndex = cmb.FindString(value);
            else
                cmb.SelectedIndex = -1;
        }

        private void SetupComboBox(ComboBox cmb, bool unwireOnly = false)
        {
            cmb.TextChanged -= Cmb_TextChanged;
            if (!unwireOnly)
                cmb.TextChanged += Cmb_TextChanged;
        }

        private void Cmb_TextChanged(object sender, EventArgs e)
        {
#if DEBUG
            if ((sender as ComboBox).Name == "coCountry")
            {
                var dummy = (sender as ComboBox)?.Text;
            }
#endif
        }

        private string DebugName => $"[{this.Name}; {this.Parent?.Parent?.Name}]";

        /// <summary>
        /// Set value for ALL UI controls:<br/>
        ///  - coCountry.SelectedValue = country<br/>
        ///  - coCounty.Text ...<br/>
        ///  - coCity.Text<br/>
        ///  - coSector.Text<br/>
        ///  - coStreetType.Text<br/>
        ///  - coStreet.Text<br/>
        ///  - tbHouseNumber.Text<br/>
        ///  - tbPostalCode.Text<br/>
        ///  <br/>Calls: NapolniCounty() -> NapolniCity() -> NapolniSector() -> NapolniStreetType() -> NapolniStreet() -> NapolniNumbers() -> NapolniPost()
        /// </summary>
        public void Napolni()
        {
            blockedInit = true;

            coCountry.SelectedValue = country;
            //coCountry.SelectedIndex = coCountry.FindString(country);

            coCounty.Text = county;
            coCity.Text = city;
            coSector.Text = sector;
            coStreetType.Text = type;
            coStreet.Text = street;
            tbHouseNumber.Text = number;
            tbPostalCode.Text = post;

            //Samo enkrat zaženemo nalaganje celotne izbrane struktre pošte
            NapolniCounty("Napolni");

            blockedInit = false;
            init = true;
        }

        private void NapolniCountry()
        {
#if DEBUG
            Console.WriteLine($"PO_RO.Poste.PostaEditor.NapolniCountry: START{DebugName}");
#endif
            BlockOnIndexChanged(true);
            coCountry.Bind(BusinesLogic.Sifranti.Drzava);
            BlockOnIndexChanged(false);
        }

        private void NapolniCounty(string caller)
        {
#if DEBUG
            Console.WriteLine($"PO_RO.Poste.PostaEditor.NapolniCounty[{DebugName}]: {caller}");
#endif
            Cursor.Current = Cursors.WaitCursor;

            string value = coCounty.Text.ToUpper().Trim();
            coCounty.Text = String.Empty;

            if (IsRomania)
            {
                BlockOnIndexChanged(true);
                coCounty.FillUsing(PostneStevilke.GetCounties());
                BlockOnIndexChanged(false);

                SetSelectedIndex(coCounty, value);
                //if (!String.IsNullOrEmpty(value))
                //    coCounty.SelectedIndex = coCounty.FindString(value);
                //else
                //    coCounty.SelectedIndex = -1;
            }
            else
            {
                coCounty.DataSource = null;
                coCounty.Text = value;
            }

            NapolniCity($"NapolniCounty[{caller}]");

            Cursor.Current = Cursors.Arrow;
        }

        private void NapolniCity(string caller)
        {
#if DEBUG
            Console.WriteLine($"PO_RO.Poste.PostaEditor.NapolniCity: {caller}");
#endif
            Cursor.Current = Cursors.WaitCursor;

            string value = coCity.Text.ToUpper().Trim();
            coCity.Text = String.Empty;

            if (SelectedCounty != null)
            {
                BlockOnIndexChanged(true);
                coCity.FillUsing(PostneStevilke.GetCities(SelectedCounty));
                BlockOnIndexChanged(false);

                if (!String.IsNullOrEmpty(value))
                {
                    SetSelectedIndex(coCity, value); //coCity.SelectedIndex = coCity.FindString(value);
                    if (coCity.SelectedIndex == -1)
                        coCity.Text = value;
                }
                else
                {
                    if (coCity.Items.Count == 1)
                        coCity.SelectedIndex = 0;
                }
            }
            else
            {
                coCity.DataSource = null;
                coCity.Text = value;
            }

            NapolniSector($"NapolniCity[{caller}]");

            Cursor.Current = Cursors.Arrow;
        }

        private void NapolniSector(string caller)
        {
#if DEBUG
            Console.WriteLine($"PO_RO.Poste.PostaEditor.NapolniSector: {caller}");
#endif
            Cursor.Current = Cursors.WaitCursor;

            string value = coSector.Text.ToUpper().Trim();
            coSector.Text = String.Empty;

            if (SelectedCounty != null && SelectedCity != null)
            {
                BlockOnIndexChanged(true);
                coSector.FillUsing(PostneStevilke.GetSectors(SelectedCounty, SelectedCity));
                BlockOnIndexChanged(false);

                if (!String.IsNullOrEmpty(value))
                {
                    SetSelectedIndex(coSector, value); //coSector.SelectedIndex = coSector.FindString(value);
                    if (coSector.SelectedIndex == -1)
                        coSector.Text = value;
                }
                else
                {
                    if (coSector.Items.Count == 1)
                        coSector.SelectedIndex = 0;
                }
            }
            else
            {
                coSector.DataSource = null;
                coSector.Text = value;
            }

            NapolniStreetType($"NapolniSector[{caller}]");

            Cursor.Current = Cursors.Arrow;
        }

        private void NapolniStreetType(string caller)
        {
#if DEBUG
            Console.WriteLine($"PO_RO.Poste.PostaEditor.NapolniStreetType: {caller}");
#endif
            Cursor.Current = Cursors.WaitCursor;

            string value = coStreetType.Text.ToUpper().Trim();
            coStreetType.Text = String.Empty;

            if (SelectedCounty != null && SelectedCity != null)
            {
                BlockOnIndexChanged(true);
                coStreetType.FillUsing(PostneStevilke.GetStreetTypes(SelectedCounty, SelectedCity, SelectedSector));
                BlockOnIndexChanged(false);

                if (!String.IsNullOrEmpty(value))
                {
                    SetSelectedIndex(coStreetType, value); // coStreetType.SelectedIndex = coStreetType.FindString(value);
                    if (coStreetType.SelectedIndex == -1)
                        coStreetType.Text = value;
                }
                else
                {
                    if (coStreetType.Items.Count == 1)
                        coStreetType.SelectedIndex = 0;
                }
            }
            else
            {
                coStreetType.DataSource = null;
                coStreetType.Text = value;
            }

            NapolniStreet($"NapolniStreetType[{caller}]");

            Cursor.Current = Cursors.Arrow;
        }

        private void NapolniStreet(string caller)
        {
#if DEBUG
            Console.WriteLine($"PO_RO.Poste.PostaEditor.NapolniStreet: {caller}");
#endif
            Cursor.Current = Cursors.WaitCursor;

            string value = coStreet.Text.ToUpper().Trim();
            coStreet.Text = String.Empty;

            if (SelectedCounty != null && SelectedCity != null && SelectedStreetType != null)
            {
                BlockOnIndexChanged(true);
                coStreet.FillUsing(PostneStevilke.GetStreets(SelectedCounty, SelectedCity, SelectedSector, SelectedStreetType));
                BlockOnIndexChanged(false);

                if (!String.IsNullOrEmpty(value))
                {
                    SetSelectedIndex(coStreet, value); // coStreet.SelectedIndex = coStreet.FindString(value);
                    if (coStreet.SelectedIndex == -1)
                        coStreet.Text = value;
                }
                else
                {
                    if (coStreet.Items.Count == 1)
                        coStreet.SelectedIndex = 0;
                }
            }
            else
            {
                coStreet.DataSource = null;
                coStreet.Text = value;
            }

            NapolniNumbers($"NapolniStreet[{caller}]");

            Cursor.Current = Cursors.Arrow;
        }

        private void NapolniNumbers(string caller)
        {
#if DEBUG
            Console.WriteLine($"PO_RO.Poste.PostaEditor.NapolniNumbers: {caller}");
#endif
            Cursor.Current = Cursors.WaitCursor;

            string value = coHouseNumber.Text.ToUpper().Trim();
            coHouseNumber.Text = String.Empty;

            if (SelectedCounty != null && SelectedCity != null && SelectedStreetType != null && SelectedStreet != null)
            {
                BlockOnIndexChanged(true);
                coHouseNumber.FillUsing(PostneStevilke.GetNumbers(SelectedCounty, SelectedCity, SelectedSector, SelectedStreetType, SelectedStreet));
                BlockOnIndexChanged(false);

                if (!String.IsNullOrEmpty(tbHouseNumber.Text.Trim()))
                {
                    tbHouseNumber_Leave(null, null);
                }
                else
                {
                    if (!String.IsNullOrEmpty(value))
                        SetSelectedIndex(coHouseNumber, value); //coHouseNumber.SelectedIndex = coHouseNumber.FindString(value);
                    else
                        coHouseNumber.SelectedIndex = -1;
                }

                if (coHouseNumber.Items.Count == 1)
                {
                    coHouseNumber.SelectedIndex = 0;
                }
            }
            else
            {
                coHouseNumber.DataSource = null;
                coHouseNumber.Text = value;
            }

            NapolniPost($"NapolniNumbers[{caller}]");

            Cursor.Current = Cursors.Arrow;
        }

        private void NapolniPost(string caller)
        {
#if DEBUG
            Console.WriteLine($"PO_RO.Poste.PostaEditor.NapolniPost: {caller}");
#endif
            Cursor.Current = Cursors.WaitCursor;

            if (SelectedCounty != null && SelectedCity != null && SelectedStreetType != null && SelectedStreet != null && SelectedStreetNumber != null)
            {
                Item postalCode = PostneStevilke.GetPostalCode(SelectedCounty, SelectedCity, SelectedSector, SelectedStreetType, SelectedStreet, SelectedStreetNumber); //0 -> null
                if (postalCode != null)
                    tbPostalCode.Text = postalCode.Naziv;
            }

            CheckInput(tbPostalCode, null);

            Cursor.Current = Cursors.Arrow;
        }

        #endregion

        private void tbHouseNumber_Leave(object sender, EventArgs e)
        {
            Item num = GetStreetNumberRange(tbHouseNumber.Text.Trim());
            if (num != null)
            {
                coHouseNumber.SelectedItem = num;
                NapolniPost("tbHouseNumber_Leave");
            }
        }

        #region CheckInput

        private bool VnosZaradiPovezav;
        private string VrstaKlienta;

        public bool CheckInput(bool povezan, string vrsta)
        {
            VnosZaradiPovezav = povezan;
            VrstaKlienta = vrsta;

            return CheckInput(null, null);
        }

        public bool CheckInput(object sender, EventArgs e)
        {
            if (!init)
                return false;

            inputOK = true; //PREJ: bool inputOK = true;

            if (sender == null || sender.Equals(coCountry))
            {
                errorProvider.SetError(coCountry, String.Empty);
                if (coCountry.Enabled && String.IsNullOrEmpty(coCountry.Text.Trim()) && KontrolaPodatkaPovezani(VrstaPodatka.Drzava))
                {
                    errorProvider.SetError(coCountry, GLOBALS.Translator.TranslateMessage("Please select country!"));
                    inputOK = false;
                }
            }

            if (sender == null || sender.Equals(coCounty))
            {
                errorProvider.SetError(coCounty, String.Empty);
                if (coCountry.Enabled && coCountry.Text.Trim() == "ROMANIA" && coCounty.FindStringExact(coCounty.Text.ToUpper().Trim()) == -1)
                {
                    errorProvider.SetError(coCounty, GLOBALS.Translator.TranslateMessage("Select county from the dropdown menu"));
                    inputOK = false;
                }
            }

            if (sender == null || sender.Equals(coCity))
            {
                errorProvider.SetError(coCity, String.Empty);
                if (coCity.Enabled && String.IsNullOrEmpty(coCity.Text.Trim()) && KontrolaPodatkaPovezani(VrstaPodatka.Ulica))
                {
                    errorProvider.SetError(coCity, GLOBALS.Translator.TranslateMessage("Please enter city!"));
                    inputOK = false;
                }
            }

            if (sender == null || sender.Equals(tbPostalCode))
            {
                errorProvider.SetError(tbPostalCode, String.Empty);
                if (!tbPostalCode.ReadOnly && String.IsNullOrEmpty(tbPostalCode.Text.Trim()) && KontrolaPodatkaPovezani(VrstaPodatka.Posta))
                {
                    errorProvider.SetError(tbPostalCode, GLOBALS.Translator.TranslateMessage("Please enter post number!"));
                    inputOK = false;
                }
            }

            if (sender == null || sender.Equals(coStreet))
            {
                errorProvider.SetError(coStreet, String.Empty);
                if (String.IsNullOrEmpty(coStreet.Text.Trim()) && String.IsNullOrEmpty(tbHouseNumber.Text.Trim()) && String.IsNullOrEmpty(tbOthers.Text.Trim()) && KontrolaPodatkaPovezani(VrstaPodatka.PostaNaslov))
                {
                    errorProvider.SetError(coStreet, GLOBALS.Translator.TranslateMessage("Other or street must be entered"));
                    inputOK = false;
                }
            }

            if (sender == null || sender.Equals(coSector))
            {
                errorProvider.SetError(coSector, String.Empty);
                if (coSector.Items.Count != 0 && coSector.FindStringExact(Sector) == -1)
                {
                    errorProvider.SetError(coSector, GLOBALS.Translator.TranslateMessage("Select sector from the list"));
                    inputOK = false;
                }
            }

            return inputOK;
        }

        /// <summary>
        /// Funkcija vrne oznako ali se zahtevno polje prevereja v CheckInput metodi. Če je vnos samo zaradi povezave, se pogleda posebno nastavitev.
        /// </summary>
        /// <param name="vrsta">Vrsta podatka ki se preveja (EMŠO, davčna ...)</param>
        /// <returns>true - podatek se preverja normalno, false - preverjanje se preskoči</returns>
        private bool KontrolaPodatkaPovezani(VrstaPodatka vrsta)
        {
            if (!VnosZaradiPovezav)
                return true;

            return SupportFunctions.KontrolaPodatkaPovezani(vrsta, VrstaKlienta);
        }

        #endregion

        public Item GetStreetNumberRange(string text)
        {
            int value = ExtractNum(text);
            if (value == -1)
                return null;

            string isEven = RangeItem.IsEvenNum(value) ? "E" : "O";
            bool found = false;

            foreach (RangeItem item in coHouseNumber.Items)
            {
                foreach (RangeData range in item.Range)
                {
                    if (range.Type == isEven)
                    {
                        if (value >= range.RangeStart && value <= range.RangeEnd)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (found)
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Extracts the first contiguous sequence of digits from a string.
        /// </summary>
        /// <param name="s">The input string.</param>
        /// <returns>The extracted integer, or -1 if no digits are found.</returns>
        private static int ExtractNum(string s)
        {
            var sb = new StringBuilder();

            if (string.IsNullOrEmpty(s))
            {
                return -1;
            }

            foreach (char c in s)
            {
                if (char.IsDigit(c))
                {
                    sb.Append(c); // Append the digit to the StringBuilder
                }
                else if (sb.Length > 0)
                {
                    // If a non-digit character is encountered AND we've already collected some digits, it means the sequence of digits has ended. Break the loop.
                    break;
                }
                // If it's a non-digit and sb.Length is 0, we simply continue, effectively skipping leading non-digit characters.
            }

            if (sb.Length == 0) // After the loop, check if any digits were collected
            {
                return -1; // No digits found
            }

            return Int32.Parse(sb.ToString()); // Convert the collected digits from StringBuilder to string, then parse to int
        }

        private void Control_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsLetter(e.KeyChar))
                e.KeyChar = Char.ToUpper(e.KeyChar);
        }

        private void ComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            ((ComboBox)sender).DroppedDown = false;
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            blocked = true; // skip executing PostalCodes_SelectedIndexChanged when disposing

            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public static class ComboBoxExtender
    {
        public static void FillUsing(this ComboBox control, List<Item> sifrant)
        {
#if DEBUG
            if (control.Name == "coCountry")
            {
                var dummy = 1;
            }
            //using (var sw = new StopwatchExtension($"PO_RO.Poste.PostaEditor.FillUsing({control.Name})", true))
#endif
            {
                control.BeginUpdate();

                //In the worst case, the previous sequence (DataSource, ValueMember, DisplayMember) will raise SelectedIndexChanged 3 times in a row:
                //The solution is simple and can be summed up in three words: SET DATASOURCE LAST.
                //Contrary to what you might expect, setting DisplayMember and ValueMember does not trigger any internal validation on the part of the control that might fail if there is not yet a DataSource.
                //VIR: https://www.codeproject.com/Articles/8390/Best-Practice-for-Binding-WinForms-ListControls

                //control.DataSource = new BindingSource { DataSource = sifrant };
                control.ValueMember = "Id";
                control.DisplayMember = "Naziv";
                control.DataSource = new BindingSource { DataSource = sifrant };
                if (control.SelectedItem != null)
                    control.SelectedItem = null;

                control.AutoCompleteSource = AutoCompleteSource.ListItems;
                control.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                control.EndUpdate();
            }
        }
    }
}
