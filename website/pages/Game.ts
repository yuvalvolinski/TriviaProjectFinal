import { send } from "../utilities";

let NickName = localStorage.getItem("NickName");
let UserId = localStorage.getItem("UserId");

let gameId = await send("StartGame", UserId) ;

alert(gameId);
