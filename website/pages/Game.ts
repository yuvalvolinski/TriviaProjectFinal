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

let timeLeft = 120; 
let lastAnsTime = 120; 
const timerElement = document.getElementById("timer") as HTMLDivElement;



const countdown = setInterval(() => {
  timerElement.textContent = timeLeft.toString();

    const minutes = Math.floor(timeLeft / 60);
    const seconds = timeLeft % 60;

    const formattedTime = `${minutes.toString().padStart(2, '0')}:${seconds
      .toString()
      .padStart(2, '0')}`;

    timerElement.textContent = formattedTime;

  if (timeLeft <= 0) {
    clearInterval(countdown);
    timerElement.textContent = "הזמן תם!";
  }

  timeLeft--;
}, 1000);

ans1.onclick  = function () {
  checkAnswer(1);
};

ans2.onclick  = function () {
  checkAnswer(2);
};

ans3.onclick  = function () {
  checkAnswer(3);
};

ans4.onclick  = function () {
  checkAnswer(4);
};


if (localStorage.getItem("GameId") === null) {
    let gameId = await send("StartGame", UserId);

    localStorage.setItem("GameId" ,  gameId )
}

getQuestion();

async function  getQuestion()  {
  let question = await send("GetQuestion", localStorage.getItem("GameId")) as Question ;

  quest.innerHTML= question.Quest;
  ans1.innerHTML= question.Ans1;
  ans2.innerHTML= question.Ans2;
  ans3.innerHTML= question.Ans3;
  ans4.innerHTML= question.Ans4;
}





async function  checkAnswer(ans: number)  {

      let ansTime = lastAnsTime - timeLeft;
      lastAnsTime = timeLeft;

      console.log('ansTime', ansTime)
  
      let correctAns = await send("CheckAnswer", [localStorage.getItem("GameId"), ans, ansTime] );

      if(correctAns === 1){
        ans1.innerText = ans1.innerText + " ✅";
      }
      else if(correctAns === 2)
      { 
        ans2.innerText = ans2.innerText + " ✅";


      }
      else if(correctAns === 3)
      {
          ans3.innerText = ans3.innerText + " ✅";


      }
      else if(correctAns === 4)
      {
        ans4.innerText = ans4.innerText + " ✅";

      }
      
      await new Promise(resolve => setTimeout(resolve, 1000));

      getQuestion();
}

