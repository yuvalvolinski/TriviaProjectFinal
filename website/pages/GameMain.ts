import { send } from "../utilities";
let bLogOut = document.getElementById("bLogOut")!;
let bRules = document.getElementById("bRules")!;
let bstart = document.getElementById("bstart")!;
let Welc_text = document.getElementById("Welc_text")!;

type TopUsers  = {
    Username: string;
    MaxScore: number;
    
}



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





  let topUsers = await send("GetTopScores", "") as TopUsers;
  let container = document.getElementById("topPlayers") as HTMLDivElement;
  

  container.innerHTML = `<h3>🏆 השחקנים המובילים</h3>` + 
    topUsers.slice(0,5).map(u => 
      `<div class="player"><span>${u.Username}</span><span>${u.MaxScore}</span></div>`
    ).join('');
