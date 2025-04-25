import { send } from "../utilities";

let b_SignIn = document.querySelector("#b_SignIn") as HTMLButtonElement;
let nick = document.querySelector("#nick") as HTMLInputElement;
let password = document.querySelector("#password") as HTMLInputElement;


b_SignIn.onclick = async function(){
    
    let nick_name = nick.value;
    let user_password = password.value;
    let result;

    result = await send("SignIn",[ nick_name, user_password]) ;



    if(result == "exists"){
        alert("משתמש זה קיים, אנא  הזן משתמש אחר")

    }
    else if(result == "invalid"){
        alert("אנא הזן סיסמה יותר בטוחה, עם לפחות 5 תווים")
    }
    else {
        alert("!נרשמת בהצלחה");
        location.href = "/website/pages/main.html";

    } 


    
 
}
