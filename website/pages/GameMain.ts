import { send } from "../utilities";
let bLogOut = document.getElementById("bLogOut")!;
let bRules = document.getElementById("bRules")!;
let bstart = document.getElementById("bstart")!;
let Welc_text = document.getElementById("Welc_text")!;



bLogOut.onclick = function () {
     window.location.href = "main.html"
  };


bRules.onclick = function () {
   window.location.href = "rules.html"
}

bstart.onclick = function () {
   if (localStorage.getItem("GameId") !== null) {
      localStorage.removeItem("GameId");
    }
   window.location.href = "Game.html"
}


let NickName = localStorage.getItem("NickName");
let UserId = localStorage.getItem("UserId");

Welc_text.innerText = "Welcome Dear, "  +  NickName;

console.log( "UserId= " + UserId);



