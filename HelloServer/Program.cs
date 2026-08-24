using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

namespace HelloServer;

public class Program
{
    //# WebApplicationBuilder & WebApplication
    
    //## WebApplication ?
    // - ASP.NET Core 어플리케이션의 런타임 호스트(Host).
    // - HTTP 미들웨어 파이프라인, 엔드 포인트 라우팅등 여러
    //  네트워크 관련 기능들을 총괄하여 실행하는 핵심 인스턴스
    
    //### ASP.NET Core ?
    // - MS에서 개발한 크로스 플랫폼 오픈소스 웹 프레임워크
    // - 클라우드 환경과 현대적인 분산 시스템에 최적화된 어플리케이션
    //  을 구축하기 위한 표준 개발 플랫폼
    // - 크로스 플랫폼을 지원합니다. (Window, Linux, macOS, 각종 Docker컨테이너)
    // - 각각 다른 환경에서도 거의 비슷한 런타임 성능과 동작을 보장 합니다.
    // - 모듈식으로 구성된 클래스들을 조합하여 앱을 구성할 수 있습니다.
    
    //## WebApplicationBuilder
    // - WebApplication을 생성하는 클래스입니다.
    // - WebApplication의 초기화, 의존성 주입, 구성(Config) 설정, 생성의 역할을 맡고 있는 클래스
    
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        builder.Services.ConfigureHttpJsonOptions(
            options => options.SerializerOptions.PropertyNamingPolicy = null);
        
        WebApplication app = builder.Build();
        
        // 앱의 구성에 값을 가져온다 "Room:BroadcastPerSecond" 키의 값을, 없다면 10을 넣는다
        int perSecond = app.Configuration.GetValue("Room:BroadcastPerSecond", 10);
        
        // 앱의 구성에 값을 가져온다 "Room:LogMovesPerSecond" 키의 값을, 없다면 1을 넣는다
        int logMoves = app.Configuration.GetValue("Room:LogMovesPerSecond", 1);
        
        // 서버에 방을 추가해 줍시다.
        RoomHub hub = new RoomHub(perSecond, logMoves);
        
        app.UseWebSockets();
        app.MapGet("/ping", () => "pong");

        // 방으로 들어오는 문
        // 여기서 await하는 동안 그 사람의 연결이 살아있습니다.
        // 여기서 hub.HandleAsync => room.HandleAsync를 호출하여
        // 한 유저의 접속부터 끊김까지 바인딩해줍니다.
        app.Map("/room", async context =>
        {
            // 웹소켓으로 접속했니?
            if (context.WebSockets.IsWebSocketRequest == false)
            {
                // 아니라면 평범한 브라우저의 접속임
                // StatusCodes = 404 notfound 뭐 그런거 모였있는겁니다.
                // 표준적인 에러처리들
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("웹소켓으로 접속하시오");
                return;
            }

            // 방코드는 쿼리 스트링을 통해서 주소에 실려오도록 설계되었습니다
            // ex) ws://localhost:5000/room?code=ABCE
            
            string code = RoomHub.Normalize(context.Request.Query["code"]);
            if (string.IsNullOrEmpty(code))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("방코드 해석 불가능");
                return;
            }
            
            // 여기까지 오면 예외처리 완료된것
            // 소켓을 만들어 준다(연결을 받아준다)
            WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await hub.HandleAsync(code, socket, context.RequestAborted);
        });

        // 어플리케이션이 종료될때까지 허브가 Broadcast 루프를 돌도록 설정해준다.
        // _ : 반환형이 있지만 안쓸때 언더바 사용함
        _ = hub.BroadcastLoopAsync(app.Lifetime.ApplicationStopped);
        
        app.Run("http://0.0.0.0:5000");
    }


    private static void HttpStudy(string[] args)
    {
        // 잊지 말고 해줘야 할것이. 우리는 한글은 쓴다
        // 한글 출력을 위해 OutputEncoding을 설정해준다
        Console.OutputEncoding = Encoding.UTF8;
        
        // 웹 앱의 인스턴스를 생성하기 위해 빌더 만들어 준다
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        
        // 아래부터는 빌더 설정
        // - Services : 앱의 의존성 주입, 서비스 수명 주기 등의 설정
        // - Configuration : 환경변수, 다양한 설정 데이터 로드
        // - Environment : 실행 환경확인 및 파일 시스템 경로 등을 제공함
        // PS : 제가 웹앱을 깊게 다루지는 않는데 보통 웹 앱을 커스텀해서 
        //     제작할때 위의 내용들을 건드립니다
        
        // 기본값 첫 글자를 소문자로 바꾸는 옵션 세팅 (NickName -> nickName)
        // 유니티의 JsonUtility가 이름이 한글자라도 다르면 빈 값을 넣어버림
        // 그래서 서버쪽 Json 정책을 바꿔서 양쪽의 이름을 맞춰둡니다.
        builder.Services.ConfigureHttpJsonOptions(
            options => options.SerializerOptions.PropertyNamingPolicy = null);
        
        WebApplication app = builder.Build();

        int perSecond = app.Configuration.GetValue("Room:BroadcastPerSecond", 10);
        // 앱의 구성에 값을 가져온다 "Room:BroadcastPerSecond" 키의 값을, 없다면 10을 넣는다
        
        int logMoves = app.Configuration.GetValue("Room:LogMovesPerSecond", 1);
        // 앱의 구성에 값을 가져온다 "Room:LogMovesPerSecond" 키의 값을, 없다면 1을 넣는다

        // 웹 소켓을 받겠다고 켜 둔다. 아래 라인이 없으면 에러를 돌려준다
        app.UseWebSockets();
        
        // /ping 경로를 외부에서 요청하면 pong을 넘기겠다는 기능을 app에 추가한다
        app.MapGet("/ping", () => "pong");
        app.MapGet("/test", () => "test");
        app.MapGet("/name", () => "jaehoonKim");
        app.MapGet("/class", () => "vr9");

        // ?를 이용한 쿼리 스트링
        // 서버에서 전송된 쿼리 스트링을 이용해서 다양한 처리를 할 수 있습니다.
        app.Map("/room", async context =>
        {
            string code = context.Request.Query["code"];
            Console.WriteLine($"수신된 요청 {code}");
        });

        
        // Post란?
        // : 서버로 데이터를 전송해 리소스를 생성하거나 변경할 때 사용하는
        //  HTTP 메서드. GET은 가져오는것만 하지만 POST 서버쪽 데이터를 바꾼다거나 생성한다거나 할 수 있음
        
        // Post로 보내게 되면 Byte에서 문자열로 바꿔서 해석을 해줘야함.
        app.MapPost("/test", async context =>
        {
            using StreamReader reader = new StreamReader(context.Request.Body);
            var msg = await reader.ReadToEndAsync();
            Console.WriteLine($"POST로 들어온 데이터 : {msg}");
            // 들어온 message를 가공합니다.
            
            string responseMessage = string.Concat("서버에서 받은거 = ", msg);

            if (msg.Contains("Jay")) responseMessage += "Jay가 보낸 메세지";
            else if (msg.Contains("Guest")) responseMessage = "게스트는 이용할 수 없습니다";
            else responseMessage = "메시지를 주셔서 감사합니다";
            
            // 응답에다가 써서 보냅니다.
            await context.Response.WriteAsync(responseMessage);
        });


        app.Run("http://0.0.0.0:5000");
        
    }
}