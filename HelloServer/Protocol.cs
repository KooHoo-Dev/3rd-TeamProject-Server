namespace HelloServer;

public class User
{
    public string Id { get; set; }
    public string NickName { get; set; }
}

// 받은 글자가 어떤 종류인지 나타내는 데이터 객체
// 일반적으로 Header라고 부릅니다.
// 전달받은 데이터의 종류만 먼저 읽고, 알맞은 처리를 합니다.
public class TypeOnly
{
    public string Type { get; set; }
}

// 위치 상태를 나타내는 데이터 객체 한줄
public class PlayerState
{
    public string Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

#region 클라이언트 -> 서버 (C2S)

public class HelloMessage
{
    public string Type { get; set; }
    public string NickName { get; set; }
}

public class MoveMessage
{
    public string Type { get; set; }
    public string Id { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
}

#endregion

#region 서버 -> 클라이언트 (S2C)

public class WelcomeMessage
{
    public string Type { get; set; } = "welcome";

    public string RoomCode { get; set; }

    public User User { get; set; }
    public User[] Users { get; set; }
}

public class JoinMessage
{
    public string Type { get; set; } = "join";
    public User User { get; set; }
}

public class LeaveMessage
{
    public string Type { get; set; } = "leave";
    public string Id { get; set; }
}

public class ChatMessage
{
    public string Type { get; set; } = "chat";
    public string Id { get; set; }
    public string NickName { get; set; }
    public string Text { get; set; }
}

public class StateMessage
{
    public string Type { get; set; } = "state";
    public PlayerState[] States { get; set; }
}

#endregion