Imports xPxP.ENUMS

Public Class ENUMS
    Public Enum SQL_TYPE As Integer
        QUERY = 1
        NONQUERY = 2
    End Enum


    Public Enum ROUTE_CODE As Integer
        UNICAST = 1
        MULTICAST = 2
        CHANNEL = 3
        OTHERNET = 4
        COMMAND = 5
        HOSTMODE = 6
        PORT = 10
    End Enum


    Public Enum TRANSPORT_MODE
        REALTIME = 0
        QUEUED = 1
    End Enum

    ' For XNET Messages
    Public Enum TRANSPORT_CMD As Integer
        SET_ID = 240
        SET_CHANNEL = 241
        SET_CANCELTOKEN_TRUE = 242
        SET_CANCELTOKEN_FALSE = 245
        E
    End Enum

    Public Enum MESSAGE_TYPE As Integer
        '// 1 - 10 reserved for routing traffic
        DISCIO = 5
        PAYLOAD = 10
        HELLO = 15
        SQL = 20
        MESSAGE = 30
        CHAT = 40
        CRYPTO = 50
        VOICE = 60
        VIDEO = 70
        VOICE_AND_VIDEO = 80
        SIP_SERVICE = 90
        SECURITY = 100
        RAWPACKET = 1000
        SPRINKER = 2500
        IPADDY = 3000
        IPV4ADDY = 3001
        IPV6ADDY = 3002
        HOSTINFO = 4000
        REMOTEINFO = 4100
        FILEREQUEST = 5000


    End Enum


    Public Enum CONTROL_TYPE As Integer


        REQUEST = 10
        REPLY = 20
        ACK = 30
        HELLO = 50

        NODE_REQUEST = 75

        OK = 100
        INVITE = 101
        SYNC = 104
        HOLD = 105
        BYE = 106
        _ERROR = 200
        RESET = 220

        SQL_QUERY = 500
        SQL_NONQUERY = 501

        NO_ALARM = 1000
        MOTION_DETECT = 1010
        DOOR_CONTACT = 1020
        WINDOW_CONTACT = 1030
        WINDOW_BREAK = 1040
        FIRE_DETECT = 1050

        SYSTEM_DISARM = 2000
        SYSTEM_ARM_1 = 2001
        SYSTEM_ARM_2 = 2002
        SYSTEM_ARM_3 = 2003
        SYSTEM_ARM_4 = 2004
        SYSTEM_ARM_5 = 2005

        SPRINKLER_RELAY_ON = 3000
        SPRINKLER_RELAY_OFF = 3001

        SIP_DIAL = 4000
        SIP_HANGUP = 4001
        SIP_ANSWER = 4002
        SIP_RINGING = 4003
        SIP_ANSWERED = 4004
        SIP_FAILED = 4005
        SIP_ERROR = 4006

        DISKIO_CREATESECTOR = 5000
        DISKIO_CREATEFILE = 5010
        DISKIO_WRITEFILE = 5020
        DISKIO_READFILE = 5030
        DISKIO_DELETEFILE = 5040
        DISKIO_REQUESTDISCSPACE = 5050
        DISKIO_RETURNBUFFER = 6050
        DISKIO_RETURNSECTORID = 6060
        DISKIO_RETURNDISCSPACE = 6070



    End Enum

    Public Enum EXTRA_CODE
        ALARM1 = 101
        ALARM2 = 102
        ALARM3 = 103
        ALARM4 = 104
        ALARM5 = 105

        START_STATION = 500
        STOP_STATION = 501

    End Enum


    Public Enum PAYLOAD_TYPE As Integer
        NONE = 0
        MP3 = 1
        FILE = 2
        SQL = 3
        MESSAGE = 4
        CHAT = 5
        VOICE = 6
        VIDEO = 7
    End Enum

    Public Enum PAYLOAD_CODES As Integer

        FILESAVE = 100

    End Enum

    Public Enum HOST_TYPE As Integer
        HOST = 5000
        HOST_AUDIO = 5001
        HOST_VIDEO = 5002
        HOST_ROUTER = 5003
        HOST_FILE = 5004
        HOST_SQL = 5005
        HOST_LORA = 5006
        HOST_SMS = 5007
        HOST_VOIP = 5008
        HOST_SIP = 5009
    End Enum

    Public Enum CLIENT_TYPE As Integer
        CLIENT = 6000
    End Enum

    Public Enum SERVICE_TYPE As Integer

        SERVICE_ROOT = 10
        SERVICE_AUTH = 30
        SERVICE_WEB = 1000
        SERVICE_FILE = 1010
        SERVICE_LORA = 1020
        SERVICE_SMS = 1030
        SERVICE_SIP = 1040
        SERVICE_DBASE = 1050
        SERVICE_ROUTE = 3010
        SERVICE_MESSAGE = 3020
        SERVICE_FORUM = 3050
        SERVICE_IMAIL = 3060
        SERVUCE_VMAIL = 4000
        SERVICE_VIDEO_HOST = 5000
        SERVICE_VIDEO_NODE = 5010
        SERVICE_AUDIO_HOST = 5025
        SERVICE_AUDIO_NODE = 5050
        SERVICE_ALARM_DETECT = 7000
        SERVICE_ALARM_PANEL = 7010
        SERVICE_ALARM = 7015

    End Enum

    Public Class XClient
        Public clienttype As CLIENT_TYPE
        Public hostid As Integer
        Public hostip As String
        Public hostport As Integer
        Public channel As Integer
        Public packetburst As Integer
        Public netlatency As Integer
        Public localid As Integer
        Public localname As String
        Public useipv6 As Boolean
        Public broadcast As Boolean
        Public mtu As Integer

    End Class

    Public Class XHost
        Public hosttype As HOST_TYPE
        Public ip As String
        Public port As Integer
        Public channel As Integer
        Public portmin As Integer
        Public portmax As Integer
        Public packetburst As Integer
        Public netlatency As Integer
        Public hostid As Integer
        Public useipv6 As Boolean
        Public mtu As Integer

    End Class

End Class


