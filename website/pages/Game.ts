import { send } from "../utilities";

type Question = {
    Id: number;
    Quest: string;
    Ans1: string;
    Ans2: string;
    Ans3: string;
    Ans4: string;
    AnsCorrect: number;
  }

let NickName = localStorage.getItem("NickName");
let UserId = localStorage.getItem("UserId");
let gameId = null;

let quest = document.getElementById("quest") as HTMLDivElement;
let ans1 = document.getElementById("ans1") as HTMLDivElement;
let ans2 = document.getElementById("ans2") as HTMLDivElement;
let ans3 = document.getElementById("ans3") as HTMLDivElement;
let ans4 = document.getElementById("ans4") as HTMLDivElement;




if (localStorage.getItem("GameId") === null) {
    let gameId = await send("StartGame", UserId);

    localStorage.setItem("GameId" ,  gameId )
}



let question = await send("GetQuestion", localStorage.getItem("GameId")) as Question ;

quest.innerHTML= question.Quest;
ans1.innerHTML= question.Ans1;
ans2.innerHTML= question.Ans2;
ans3.innerHTML= question.Ans3;
ans4.innerHTML= question.Ans4;





