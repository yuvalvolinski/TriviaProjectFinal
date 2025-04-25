import { send } from "../utilities";



let b_Login = document.querySelector("#b_Login") as HTMLButtonElement;
let nick_name = document.querySelector("#nick_name") as HTMLInputElement;
let password = document.querySelector("#password") as HTMLInputElement;




b_Login.onclick = async function(){
    let nick = nick_name.value;
    let user_password = password.value;
    let  result;     

    result = await send("Login",[ nick, user_password]) ;

    

    if(result != "Error"){
        localStorage.setItem("NickName", nick);
        localStorage.setItem("UserId", result);
        location.href = "/website/pages/GameMain.html";

    }
    else{
        alert("פרטי המשתמש או הסיסמא אינם תקינים");
    }

}


