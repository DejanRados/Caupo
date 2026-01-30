// Cis.Fiscalization v1.3.0 :: CIS WSDL v1.4 (2012-2017)
// https://github.com/tgrospic/Cis.Fiscalization
// Copyright (c) 2013-present Tomislav Grospic
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using System.Xml.Serialization;
using System.ComponentModel;
using System.CodeDom.Compiler;

namespace Caupo.Cis
{
    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (AnonymousType = true, Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class RacunZahtjev
    {
        private ZaglavljeType zaglavljeField;
        private RacunType racunField;
        private SignatureType signatureField;
        private string idField;

        /// <remarks/>
        public ZaglavljeType Zaglavlje
        {
            get { return this.zaglavljeField; }
            set { this.zaglavljeField = value; }
        }

        /// <remarks/>
        public RacunType Racun
        {
            get { return this.racunField; }
            set { this.racunField = value; }
        }

        /// <remarks/>
        [XmlElement (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public SignatureType Signature
        {
            get { return this.signatureField; }
            set { this.signatureField = value; }
        }

        /// <remarks/>
        [XmlAttribute ()]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class ZaglavljeType
    {
        private string idPorukeField;
        private string datumVrijemeField;

        /// <remarks/>
        public string IdPoruke
        {
            get { return this.idPorukeField; }
            set { this.idPorukeField = value; }
        }

        /// <remarks/>
        public string DatumVrijeme
        {
            get { return this.datumVrijemeField; }
            set { this.datumVrijemeField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class GreskaType
    {
        private string sifraGreskeField;
        private string porukaGreskeField;

        /// <remarks/>
        public string SifraGreske
        {
            get { return this.sifraGreskeField; }
            set { this.sifraGreskeField = value; }
        }

        /// <remarks/>
        public string PorukaGreske
        {
            get { return this.porukaGreskeField; }
            set { this.porukaGreskeField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class ZaglavljeOdgovorType
    {
        private string idPorukeField;
        private string datumVrijemeField;

        /// <remarks/>
        public string IdPoruke
        {
            get { return this.idPorukeField; }
            set { this.idPorukeField = value; }
        }

        /// <remarks/>
        public string DatumVrijeme
        {
            get { return this.datumVrijemeField; }
            set { this.datumVrijemeField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class ObjectType
    {
        private System.Xml.XmlNode[] anyField;
        private string idField;
        private string mimeTypeField;
        private string encodingField;

        /// <remarks/>
        [XmlText ()]
        [XmlAnyElement ()]
        public System.Xml.XmlNode[] Any
        {
            get { return this.anyField; }
            set { this.anyField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "ID")]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }

        /// <remarks/>
        [XmlAttribute ()]
        public string MimeType
        {
            get { return this.mimeTypeField; }
            set { this.mimeTypeField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string Encoding
        {
            get { return this.encodingField; }
            set { this.encodingField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class SPKIDataType
    {
        private byte[][] sPKISexpField;
        private System.Xml.XmlElement anyField;

        /// <remarks/>
        [XmlElement ("SPKISexp", DataType = "base64Binary")]
        public byte[][] SPKISexp
        {
            get { return this.sPKISexpField; }
            set { this.sPKISexpField = value; }
        }

        /// <remarks/>
        [XmlAnyElement ()]
        public System.Xml.XmlElement Any
        {
            get { return this.anyField; }
            set { this.anyField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class PGPDataType
    {
        private object[] itemsField;
        private ItemsChoiceType1[] itemsElementNameField;

        /// <remarks/>
        [XmlAnyElement ()]
        [XmlElement ("PGPKeyID", typeof (byte[]), DataType = "base64Binary")]
        [XmlElement ("PGPKeyPacket", typeof (byte[]), DataType = "base64Binary")]
        [XmlChoiceIdentifier ("ItemsElementName")]
        public object[] Items
        {
            get { return this.itemsField; }
            set { this.itemsField = value; }
        }

        /// <remarks/>
        [XmlElement ("ItemsElementName")]
        [XmlIgnore ()]
        public ItemsChoiceType1[] ItemsElementName
        {
            get { return this.itemsElementNameField; }
            set { this.itemsElementNameField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [Serializable ()]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#", IncludeInSchema = false)]
    public enum ItemsChoiceType1
    {
        /// <remarks/>
        [XmlEnum ("##any:")]
        Item,
        /// <remarks/>
        PGPKeyID,
        /// <remarks/>
        PGPKeyPacket,
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class X509IssuerSerialType
    {
        private string x509IssuerNameField;
        private string x509SerialNumberField;

        /// <remarks/>
        public string X509IssuerName
        {
            get { return this.x509IssuerNameField; }
            set { this.x509IssuerNameField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "integer")]
        public string X509SerialNumber
        {
            get { return this.x509SerialNumberField; }
            set { this.x509SerialNumberField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class X509DataType
    {
        private object[] itemsField;
        private ItemsChoiceType[] itemsElementNameField;

        /// <remarks/>
        [XmlAnyElement ()]
        [XmlElement ("X509CRL", typeof (byte[]), DataType = "base64Binary")]
        [XmlElement ("X509Certificate", typeof (byte[]), DataType = "base64Binary")]
        [XmlElement ("X509IssuerSerial", typeof (X509IssuerSerialType))]
        [XmlElement ("X509SKI", typeof (byte[]), DataType = "base64Binary")]
        [XmlElement ("X509SubjectName", typeof (string))]
        [XmlChoiceIdentifier ("ItemsElementName")]
        public object[] Items
        {
            get { return this.itemsField; }
            set { this.itemsField = value; }
        }

        /// <remarks/>
        [XmlElement ("ItemsElementName")]
        [XmlIgnore ()]
        public ItemsChoiceType[] ItemsElementName
        {
            get { return this.itemsElementNameField; }
            set { this.itemsElementNameField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [Serializable ()]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#", IncludeInSchema = false)]
    public enum ItemsChoiceType
    {
        /// <remarks/>
        [XmlEnum ("##any:")]
        Item,
        /// <remarks/>
        X509CRL,
        /// <remarks/>
        X509Certificate,
        /// <remarks/>
        X509IssuerSerial,
        /// <remarks/>
        X509SKI,
        /// <remarks/>
        X509SubjectName,
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class RetrievalMethodType
    {
        private TransformType[] transformsField;
        private string uRIField;
        private string typeField;

        /// <remarks/>
        [XmlArrayItem ("Transform", IsNullable = false)]
        public TransformType[] Transforms
        {
            get { return this.transformsField; }
            set { this.transformsField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string URI
        {
            get { return this.uRIField; }
            set { this.uRIField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string Type
        {
            get { return this.typeField; }
            set { this.typeField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class TransformType
    {
        private object[] itemsField;
        private string[] textField;
        private string algorithmField;

        /// <remarks/>
        [XmlAnyElement ()]
        [XmlElement ("XPath", typeof (string))]
        public object[] Items
        {
            get { return this.itemsField; }
            set { this.itemsField = value; }
        }

        /// <remarks/>
        [XmlText ()]
        public string[] Text
        {
            get { return this.textField; }
            set { this.textField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string Algorithm
        {
            get { return this.algorithmField; }
            set { this.algorithmField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class RSAKeyValueType
    {
        private byte[] modulusField;
        private byte[] exponentField;

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] Modulus
        {
            get { return this.modulusField; }
            set { this.modulusField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] Exponent
        {
            get { return this.exponentField; }
            set { this.exponentField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class DSAKeyValueType
    {
        private byte[] pField;
        private byte[] qField;
        private byte[] gField;
        private byte[] yField;
        private byte[] jField;
        private byte[] seedField;
        private byte[] pgenCounterField;

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] P
        {
            get { return this.pField; }
            set { this.pField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] Q
        {
            get { return this.qField; }
            set { this.qField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] G
        {
            get { return this.gField; }
            set { this.gField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] Y
        {
            get { return this.yField; }
            set { this.yField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] J
        {
            get { return this.jField; }
            set { this.jField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] Seed
        {
            get { return this.seedField; }
            set { this.seedField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] PgenCounter
        {
            get { return this.pgenCounterField; }
            set { this.pgenCounterField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class KeyValueType
    {
        private object itemField;
        private string[] textField;

        /// <remarks/>
        [XmlAnyElement ()]
        [XmlElement ("DSAKeyValue", typeof (DSAKeyValueType))]
        [XmlElement ("RSAKeyValue", typeof (RSAKeyValueType))]
        public object Item
        {
            get { return this.itemField; }
            set { this.itemField = value; }
        }

        /// <remarks/>
        [XmlText ()]
        public string[] Text
        {
            get { return this.textField; }
            set { this.textField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class KeyInfoType
    {
        private object[] itemsField;
        private ItemsChoiceType2[] itemsElementNameField;
        private string[] textField;
        private string idField;

        /// <remarks/>
        [XmlAnyElement ()]
        [XmlElement ("KeyName", typeof (string))]
        [XmlElement ("KeyValue", typeof (KeyValueType))]
        [XmlElement ("MgmtData", typeof (string))]
        [XmlElement ("PGPData", typeof (PGPDataType))]
        [XmlElement ("RetrievalMethod", typeof (RetrievalMethodType))]
        [XmlElement ("SPKIData", typeof (SPKIDataType))]
        [XmlElement ("X509Data", typeof (X509DataType))]
        [XmlChoiceIdentifier ("ItemsElementName")]
        public object[] Items
        {
            get { return this.itemsField; }
            set { this.itemsField = value; }
        }

        /// <remarks/>
        [XmlElement ("ItemsElementName")]
        [XmlIgnore ()]
        public ItemsChoiceType2[] ItemsElementName
        {
            get { return this.itemsElementNameField; }
            set { this.itemsElementNameField = value; }
        }

        /// <remarks/>
        [XmlText ()]
        public string[] Text
        {
            get { return this.textField; }
            set { this.textField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "ID")]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [Serializable ()]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#", IncludeInSchema = false)]
    public enum ItemsChoiceType2
    {
        /// <remarks/>
        [XmlEnum ("##any:")]
        Item,
        /// <remarks/>
        KeyName,
        /// <remarks/>
        KeyValue,
        /// <remarks/>
        MgmtData,
        /// <remarks/>
        PGPData,
        /// <remarks/>
        RetrievalMethod,
        /// <remarks/>
        SPKIData,
        /// <remarks/>
        X509Data,
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class SignatureValueType
    {
        private string idField;
        private byte[] valueField;

        /// <remarks/>
        [XmlAttribute (DataType = "ID")]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }

        /// <remarks/>
        [XmlText (DataType = "base64Binary")]
        public byte[] Value
        {
            get { return this.valueField; }
            set { this.valueField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class DigestMethodType
    {
        private System.Xml.XmlNode[] anyField;
        private string algorithmField;

        /// <remarks/>
        [XmlText ()]
        [XmlAnyElement ()]
        public System.Xml.XmlNode[] Any
        {
            get { return this.anyField; }
            set { this.anyField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string Algorithm
        {
            get { return this.algorithmField; }
            set { this.algorithmField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class ReferenceType
    {
        private TransformType[] transformsField;
        private DigestMethodType digestMethodField;
        private byte[] digestValueField;
        private string idField;
        private string uRIField;
        private string typeField;

        /// <remarks/>
        [XmlArrayItem ("Transform", IsNullable = false)]
        public TransformType[] Transforms
        {
            get { return this.transformsField; }
            set { this.transformsField = value; }
        }

        /// <remarks/>
        public DigestMethodType DigestMethod
        {
            get { return this.digestMethodField; }
            set { this.digestMethodField = value; }
        }

        /// <remarks/>
        [XmlElement (DataType = "base64Binary")]
        public byte[] DigestValue
        {
            get { return this.digestValueField; }
            set { this.digestValueField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "ID")]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string URI
        {
            get { return this.uRIField; }
            set { this.uRIField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string Type
        {
            get { return this.typeField; }
            set { this.typeField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class SignatureMethodType
    {
        private string hMACOutputLengthField;
        private System.Xml.XmlNode[] anyField;
        private string algorithmField;

        /// <remarks/>
        [XmlElement (DataType = "integer")]
        public string HMACOutputLength
        {
            get { return this.hMACOutputLengthField; }
            set { this.hMACOutputLengthField = value; }
        }

        /// <remarks/>
        [XmlText ()]
        [XmlAnyElement ()]
        public System.Xml.XmlNode[] Any
        {
            get { return this.anyField; }
            set { this.anyField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string Algorithm
        {
            get { return this.algorithmField; }
            set { this.algorithmField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class CanonicalizationMethodType
    {
        private System.Xml.XmlNode[] anyField;
        private string algorithmField;

        /// <remarks/>
        [XmlText ()]
        [XmlAnyElement ()]
        public System.Xml.XmlNode[] Any
        {
            get { return this.anyField; }
            set { this.anyField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "anyURI")]
        public string Algorithm
        {
            get { return this.algorithmField; }
            set { this.algorithmField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class SignedInfoType
    {
        private CanonicalizationMethodType canonicalizationMethodField;
        private SignatureMethodType signatureMethodField;
        private ReferenceType[] referenceField;
        private string idField;

        /// <remarks/>
        public CanonicalizationMethodType CanonicalizationMethod
        {
            get { return this.canonicalizationMethodField; }
            set { this.canonicalizationMethodField = value; }
        }

        /// <remarks/>
        public SignatureMethodType SignatureMethod
        {
            get { return this.signatureMethodField; }
            set { this.signatureMethodField = value; }
        }

        /// <remarks/>
        [XmlElement ("Reference")]
        public ReferenceType[] Reference
        {
            get { return this.referenceField; }
            set { this.referenceField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "ID")]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class SignatureType
    {
        private SignedInfoType signedInfoField;
        private SignatureValueType signatureValueField;
        private KeyInfoType keyInfoField;
        private ObjectType[] objectField;
        private string idField;

        /// <remarks/>
        public SignedInfoType SignedInfo
        {
            get { return this.signedInfoField; }
            set { this.signedInfoField = value; }
        }

        /// <remarks/>
        public SignatureValueType SignatureValue
        {
            get { return this.signatureValueField; }
            set { this.signatureValueField = value; }
        }

        /// <remarks/>
        public KeyInfoType KeyInfo
        {
            get { return this.keyInfoField; }
            set { this.keyInfoField = value; }
        }

        /// <remarks/>
        [XmlElement ("Object")]
        public ObjectType[] Object
        {
            get { return this.objectField; }
            set { this.objectField = value; }
        }

        /// <remarks/>
        [XmlAttribute (DataType = "ID")]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class NaknadaType
    {
        private string nazivNField;
        private string iznosNField;

        /// <remarks/>
        public string NazivN
        {
            get { return this.nazivNField; }
            set { this.nazivNField = value; }
        }

        /// <remarks/>
        public string IznosN
        {
            get { return this.iznosNField; }
            set { this.iznosNField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class PorezOstaloType
    {
        private string nazivField;
        private string stopaField;
        private string osnovicaField;
        private string iznosField;

        /// <remarks/>
        public string Naziv
        {
            get { return this.nazivField; }
            set { this.nazivField = value; }
        }

        /// <remarks/>
        public string Stopa
        {
            get { return this.stopaField; }
            set { this.stopaField = value; }
        }

        /// <remarks/>
        public string Osnovica
        {
            get { return this.osnovicaField; }
            set { this.osnovicaField = value; }
        }

        /// <remarks/>
        public string Iznos
        {
            get { return this.iznosField; }
            set { this.iznosField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class PorezType
    {
        private string stopaField;
        private string osnovicaField;
        private string iznosField;

        /// <remarks/>
        public string Stopa
        {
            get { return this.stopaField; }
            set { this.stopaField = value; }
        }

        /// <remarks/>
        public string Osnovica
        {
            get { return this.osnovicaField; }
            set { this.osnovicaField = value; }
        }

        /// <remarks/>
        public string Iznos
        {
            get { return this.iznosField; }
            set { this.iznosField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class BrojRacunaType
    {
        private string brOznRacField;
        private string oznPosPrField;
        private string oznNapUrField;

        /// <remarks/>
        public string BrOznRac
        {
            get { return this.brOznRacField; }
            set { this.brOznRacField = value; }
        }

        /// <remarks/>
        public string OznPosPr
        {
            get { return this.oznPosPrField; }
            set { this.oznPosPrField = value; }
        }

        /// <remarks/>
        public string OznNapUr
        {
            get { return this.oznNapUrField; }
            set { this.oznNapUrField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class RacunType
    {
        [XmlAttribute ("Id", DataType = "ID")]
        public string Id { get; set; }
        private string oibField;
        private bool uSustPdvField;
        private string datVrijemeField;
        [XmlIgnore] 
        public string DatVrijemeZki
        {
            get { return DatVrijeme?.Replace ("t", ""); } // Za ZKI: "22.01.202416:28:21"
        }
        private OznakaSlijednostiType oznSlijedField;
        private BrojRacunaType brRacField;
        private PorezType[] pdvField;
        private PorezType[] pnpField;
        private PorezOstaloType[] ostaliPorField;
        private string iznosOslobPdvField;
        private string iznosMarzaField;
        private string iznosNePodlOporField;
        private NaknadaType[] naknadeField;
        private string iznosUkupnoField;
        private NacinPlacanjaType nacinPlacField;
        private string oibOperField;
        private string zastKodField;
        private bool nakDostField;
        private string paragonBrRacField;
        private string specNamjField;

        /// <remarks/>
        public string Oib
        {
            get { return this.oibField; }
            set { this.oibField = value; }
        }

        /// <remarks/>
        public bool USustPdv
        {
            get { return this.uSustPdvField; }
            set { this.uSustPdvField = value; }
        }

        /// <remarks/>
        public string DatVrijeme
        {
            get { return this.datVrijemeField; }
            set { this.datVrijemeField = value; }
        }

        /// <remarks/>
        public OznakaSlijednostiType OznSlijed
        {
            get { return this.oznSlijedField; }
            set { this.oznSlijedField = value; }
        }

        /// <remarks/>
        public BrojRacunaType BrRac
        {
            get { return this.brRacField; }
            set { this.brRacField = value; }
        }

        /// <remarks/>
        [XmlArrayItem ("Porez", IsNullable = false)]
        public PorezType[] Pdv
        {
            get { return this.pdvField; }
            set { this.pdvField = value; }
        }

        /// <remarks/>
        [XmlArrayItem ("Porez", IsNullable = false)]
        public PorezType[] Pnp
        {
            get { return this.pnpField; }
            set { this.pnpField = value; }
        }

        /// <remarks/>
        [XmlArrayItem ("Porez", IsNullable = false)]
        public PorezOstaloType[] OstaliPor
        {
            get { return this.ostaliPorField; }
            set { this.ostaliPorField = value; }
        }

        /// <remarks/>
        public string IznosOslobPdv
        {
            get { return this.iznosOslobPdvField; }
            set { this.iznosOslobPdvField = value; }
        }

        /// <remarks/>
        public string IznosMarza
        {
            get { return this.iznosMarzaField; }
            set { this.iznosMarzaField = value; }
        }

        /// <remarks/>
        public string IznosNePodlOpor
        {
            get { return this.iznosNePodlOporField; }
            set { this.iznosNePodlOporField = value; }
        }

        /// <remarks/>
        [XmlArrayItem ("Naknada", IsNullable = false)]
        public NaknadaType[] Naknade
        {
            get { return this.naknadeField; }
            set { this.naknadeField = value; }
        }

        /// <remarks/>
        public string IznosUkupno
        {
            get { return this.iznosUkupnoField; }
            set { this.iznosUkupnoField = value; }
        }

        /// <remarks/>
        public NacinPlacanjaType NacinPlac
        {
            get { return this.nacinPlacField; }
            set { this.nacinPlacField = value; }
        }

        /// <remarks/>
        public string OibOper
        {
            get { return this.oibOperField; }
            set { this.oibOperField = value; }
        }

        /// <remarks/>
        public string ZastKod
        {
            get { return this.zastKodField; }
            set { this.zastKodField = value; }
        }

        /// <remarks/>
        public bool NakDost
        {
            get { return this.nakDostField; }
            set { this.nakDostField = value; }
        }

        /// <remarks/>
        public string ParagonBrRac
        {
            get { return this.paragonBrRacField; }
            set { this.paragonBrRacField = value; }
        }

        /// <remarks/>
        public string SpecNamj
        {
            get { return this.specNamjField; }
            set { this.specNamjField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [Serializable ()]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public enum OznakaSlijednostiType
    {
        /// <remarks/>
        N,
        /// <remarks/>
        P,
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [Serializable ()]
    [XmlType (Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public enum NacinPlacanjaType
    {
        /// <remarks/>
        G,
        /// <remarks/>
        K,
        /// <remarks/>
        C,
        /// <remarks/>
        T,
        /// <remarks/>
        O,
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (AnonymousType = true, Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class RacunOdgovor
    {
        private ZaglavljeOdgovorType zaglavljeField;
        private string jirField;
        private GreskaType[] greskeField;
        private SignatureType signatureField;
        private string idField;

        /// <remarks/>
        public ZaglavljeOdgovorType Zaglavlje
        {
            get { return this.zaglavljeField; }
            set { this.zaglavljeField = value; }
        }

        /// <remarks/>
        public string Jir
        {
            get { return this.jirField; }
            set { this.jirField = value; }
        }

        /// <remarks/>
        [XmlArrayItem ("Greska", IsNullable = false)]
        public GreskaType[] Greske
        {
            get { return this.greskeField; }
            set { this.greskeField = value; }
        }

        /// <remarks/>
        [XmlElement (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public SignatureType Signature
        {
            get { return this.signatureField; }
            set { this.signatureField = value; }
        }

        /// <remarks/>
        [XmlAttribute ()]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (AnonymousType = true, Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class ProvjeraZahtjev
    {
        private ZaglavljeType zaglavljeField;
        private RacunType racunField;
        private SignatureType signatureField;
        private string idField;

        /// <remarks/>
        public ZaglavljeType Zaglavlje
        {
            get { return this.zaglavljeField; }
            set { this.zaglavljeField = value; }
        }

        /// <remarks/>
        public RacunType Racun
        {
            get { return this.racunField; }
            set { this.racunField = value; }
        }

        /// <remarks/>
        [XmlElement (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public SignatureType Signature
        {
            get { return this.signatureField; }
            set { this.signatureField = value; }
        }

        /// <remarks/>
        [XmlAttribute ()]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }
    }

    /// <remarks/>
    [GeneratedCode ("wsdl", "4.6.81.0")]
    [DebuggerStepThrough ()]
    [DesignerCategory ("code")]
    [XmlType (AnonymousType = true, Namespace = "http://www.apis-it.hr/fin/2012/types/f73")]
    public partial class ProvjeraOdgovor
    {
        private ZaglavljeOdgovorType zaglavljeField;
        private RacunType racunField;
        private GreskaType[] greskeField;
        private SignatureType signatureField;
        private string idField;

        /// <remarks/>
        public ZaglavljeOdgovorType Zaglavlje
        {
            get { return this.zaglavljeField; }
            set { this.zaglavljeField = value; }
        }

        /// <remarks/>
        public RacunType Racun
        {
            get { return this.racunField; }
            set { this.racunField = value; }
        }

        /// <remarks/>
        [XmlArrayItem ("Greska", IsNullable = false)]
        public GreskaType[] Greske
        {
            get { return this.greskeField; }
            set { this.greskeField = value; }
        }

        /// <remarks/>
        [XmlElement (Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public SignatureType Signature
        {
            get { return this.signatureField; }
            set { this.signatureField = value; }
        }

        /// <remarks/>
        [XmlAttribute ()]
        public string Id
        {
            get { return this.idField; }
            set { this.idField = value; }
        }
    }
}