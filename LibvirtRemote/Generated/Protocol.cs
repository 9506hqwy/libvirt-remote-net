//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Protocol {
    
    
    public class Constants {
        
        public const uint VirNetMessageInitial = 65536u;
        
        public const uint VirNetMessageLegacyPayloadMax = 262120u;
        
        public const uint VirNetMessageMax = 33554432u;
        
        public const uint VirNetMessageHeaderMax = 24u;
        
        public const uint VirNetMessagePayloadMax = 33554408u;
        
        public const uint VirNetMessageLenMax = 4u;
        
        public const uint VirNetMessageStringMax = 4194304u;
        
        public const uint VirNetMessageNumFdsMax = 32u;
        
        public const uint VirNetMessageHeaderXdrLen = 4u;
        
        public const uint VirUuidBuflen = 16u;
    }
    
    [System.SerializableAttribute()]
    public enum VirNetMessageType {
        
        VirNetCall = 0,
        
        VirNetReply = 1,
        
        VirNetMessage = 2,
        
        VirNetStream = 3,
        
        VirNetCallWithFds = 4,
        
        VirNetReplyWithFds = 5,
        
        VirNetStreamHole = 6,
    }
    
    [System.SerializableAttribute()]
    public enum VirNetMessageStatus {
        
        VirNetOk = 0,
        
        VirNetError = 1,
        
        VirNetContinue = 2,
    }
    
    [System.SerializableAttribute()]
    [Xdr.XdrStructAttribute()]
    public partial class VirNetMessageHeader {
        
        private uint prog;
        
        private uint vers;
        
        private int proc;
        
        private VirNetMessageType type;
        
        private uint serial;
        
        private VirNetMessageStatus status;
        
        [Xdr.XdrElementOrderAttribute(1)]
        public uint Prog {
            get {
                return this.prog;
            }
            set {
                this.prog = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(2)]
        public uint Vers {
            get {
                return this.vers;
            }
            set {
                this.vers = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(3)]
        public int Proc {
            get {
                return this.proc;
            }
            set {
                this.proc = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(4)]
        public VirNetMessageType Type {
            get {
                return this.type;
            }
            set {
                this.type = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(5)]
        public uint Serial {
            get {
                return this.serial;
            }
            set {
                this.serial = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(6)]
        public VirNetMessageStatus Status {
            get {
                return this.status;
            }
            set {
                this.status = value;
            }
        }
    }
    
    [System.SerializableAttribute()]
    [Xdr.XdrStructAttribute()]
    public partial class VirNetMessageNonnullDomain {
        
        private string name;
        
        private byte[] uuid;
        
        private int id;
        
        [Xdr.XdrElementOrderAttribute(1)]
        public string Name {
            get {
                return this.name;
            }
            set {
                this.name = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(2)]
        [Xdr.XdrFixedLengthAttribute(16)]
        public byte[] Uuid {
            get {
                return this.uuid;
            }
            set {
                this.uuid = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(3)]
        public int Id {
            get {
                return this.id;
            }
            set {
                this.id = value;
            }
        }
    }
    
    [System.SerializableAttribute()]
    [Xdr.XdrStructAttribute()]
    public partial class VirNetMessageNonnullNetwork {
        
        private string name;
        
        private byte[] uuid;
        
        [Xdr.XdrElementOrderAttribute(1)]
        public string Name {
            get {
                return this.name;
            }
            set {
                this.name = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(2)]
        [Xdr.XdrFixedLengthAttribute(16)]
        public byte[] Uuid {
            get {
                return this.uuid;
            }
            set {
                this.uuid = value;
            }
        }
    }
    
    [System.SerializableAttribute()]
    [Xdr.XdrStructAttribute()]
    public partial class VirNetMessageError {
        
        private int code;
        
        private int domain;
        
        private Xdr.XdrOption<string> message;
        
        private int level;
        
        private Xdr.XdrOption<VirNetMessageNonnullDomain> dom;
        
        private Xdr.XdrOption<string> str1;
        
        private Xdr.XdrOption<string> str2;
        
        private Xdr.XdrOption<string> str3;
        
        private int int1;
        
        private int int2;
        
        private Xdr.XdrOption<VirNetMessageNonnullNetwork> net;
        
        [Xdr.XdrElementOrderAttribute(1)]
        public int Code {
            get {
                return this.code;
            }
            set {
                this.code = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(2)]
        public int Domain {
            get {
                return this.domain;
            }
            set {
                this.domain = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(3)]
        public Xdr.XdrOption<string> Message {
            get {
                return this.message;
            }
            set {
                this.message = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(4)]
        public int Level {
            get {
                return this.level;
            }
            set {
                this.level = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(5)]
        public Xdr.XdrOption<VirNetMessageNonnullDomain> Dom {
            get {
                return this.dom;
            }
            set {
                this.dom = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(6)]
        public Xdr.XdrOption<string> Str1 {
            get {
                return this.str1;
            }
            set {
                this.str1 = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(7)]
        public Xdr.XdrOption<string> Str2 {
            get {
                return this.str2;
            }
            set {
                this.str2 = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(8)]
        public Xdr.XdrOption<string> Str3 {
            get {
                return this.str3;
            }
            set {
                this.str3 = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(9)]
        public int Int1 {
            get {
                return this.int1;
            }
            set {
                this.int1 = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(10)]
        public int Int2 {
            get {
                return this.int2;
            }
            set {
                this.int2 = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(11)]
        public Xdr.XdrOption<VirNetMessageNonnullNetwork> Net {
            get {
                return this.net;
            }
            set {
                this.net = value;
            }
        }
    }
    
    [System.SerializableAttribute()]
    [Xdr.XdrStructAttribute()]
    public partial class VirNetStreamHole {
        
        private long length;
        
        private uint flags;
        
        [Xdr.XdrElementOrderAttribute(1)]
        public long Length {
            get {
                return this.length;
            }
            set {
                this.length = value;
            }
        }
        
        [Xdr.XdrElementOrderAttribute(2)]
        public uint Flags {
            get {
                return this.flags;
            }
            set {
                this.flags = value;
            }
        }
    }
}
