import http from "k6/http";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import {
  randomString,
} from 'https://jslib.k6.io/k6-utils/1.2.0/index.js';

// export let options = {
//   insecureSkipTLSVerify: true,
//   noConnectionReuse: false,
//   stages: [
//     { duration: '1m', target: 100 },
//     { duration: '2m', target: 100},
//     { duration: '1m', target: 0 },
//   ]
// }

// const newEventData =
//     {
//       title: "dasdasdasd",
//       description: "asdasdasd",
//       location: "asdasdasd",
//       start: {
//         date: {
//           year: (new Date).getFullYear(),
//           month: 6,
//           day: 19
//         },
//         time: {
//           hour: 17,
//           minute: 0
//         }
//       },
//       end: {
//         date: {
//           year: (new Date).getFullYear(),
//           month: 6,
//           day: 19
//         },
//         time: {
//           hour: 20,
//           minute: 0
//         }
//       },
//       openForRegistrationTime: Number(new Date()),
//       organizerName: "Name",
//       organizerEmail: "Email@email",
//       participantQuestions: [],
//       hasWaitingList: false,
//       isCancelled: false,
//       isExternal: true,
//       isHidden: false,
//       startDate: {
//         date: {
//           year: (new Date).getFullYear(),
//           month: 6,
//           day: 19
//         },
//         time: {
//           hour: 17,
//           minute: 0
//         }
//       },
//       endDate: {
//         date: {
//           year: (new Date).getFullYear(),
//           month: 6,
//           day: 19
//         },
//         time: {
//           hour: 20,
//           minute: 0
//         }
//       },
//       editUrlTemplate: "{eventId}/edit?editToken=%7BeditToken%7D"
//     }

export default function() {
  // const authenticationOptions = {
  //   headers: {
  //     Authorization: `bearer ${token}`,
  //   },
  // };
  // let foo = Object.assign(authenticationOptions, newEventData)
  // if (__ITER === 0) {
  //   let newEvent = http.post("http://localhost:5000/events", foo)
  //   console.log(newEvent.status)
  //   console.log("NEW EVENT", newEvent.json())
  //   console.log(id)
  // }
  const token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6Ik56RTVOVFJHUVRnNVJVRkNSVFJEUkRnelEwUXdORE5GUkRZeU4wWkZNVEJGUmpCRFFUVkJRUSJ9.eyJodHRwczovL2FwaS5iZWtrLm5vL2NsYWltcy9wZXJtaXNzaW9uIjpbInJlYWQ6c3RhZmZpbmciLCJ3cml0ZTpzdGFmZmluZyIsIndyaXRlOnRlbnRhdGl2U3RhZmZpbmciLCJyZWFkOnRlbnRhdGl2U3RhZmZpbmciLCJyZWFkOmVtcGxveWVlcyIsInJlYWQ6YmVrayIsIndyaXRlOmVtcGxveWVlcyIsIndyaXRlOnRpbWVjb2RlcyIsIndyaXRlOmFjY291bnRpbmciLCJhZG1pbjppbnZvaWNlLXN2YyIsImFkbWluOlNhbGVzQW5kUHJvamVjdHMtc3ZjIiwicmVhZDpvcHBvcnR1bml0eSIsIndyaXRlOm9wcG9ydHVuaXR5Iiwid3JpdGU6cHJvamVjdCIsInJlYWRXcml0ZTpwcm9nbm9zaXMiLCJyZWFkV3JpdGU6c3ViY29udHJhY3RvciIsImFkbWluOnRpbWVrZWVwZXItc3ZjIiwicmVhZDplZ2VubWVsZGluZ2VyIiwicmVhZDp0aW1lY29kZXMiLCJyZWFkV3JpdGU6dGltZXNoZWV0cyIsInJlYWQ6aW52b2ljZXMiLCJyZWFkOmN1c3RvbWVyIiwicmVhZDpwcm9qZWN0IiwicmVhZFdyaXRlOmNhbGVuZGFyIiwiYmF0Y2hVcGRhdGU6Y2FsZW5kYXIiLCJyZWFkV3JpdGU6cGFya2luZyIsInJlYWRXcml0ZTpldmVudHMiLCJ3cml0ZTpwcmFjdGljZUdyb3VwcyIsInJlYWQ6Y3YiLCJkZWxldGU6YXV0aDB1c2VyIiwiYWRtaW46Y2FiaW4iLCJyZWFkOmZvcnNpZGUiLCJyZWFkOmNhYmluIiwiYWRtaW46YXV0aCIsImFkbWluOmVtcGxveWVlLXN2YyIsImFkbWluOmFycmFuZ2VtZW50IiwicmVhZDphcnJhbmdlbWVudCIsImFkbWluOnBhcmtpbmciLCJhZG1pbjpzcGlyaXRmb25kIiwicmVhZDphY2NvdW50aW5nIiwiYWRtaW46Y2FsZW5kYXIiLCJyZWFkOlN1YkNvbnRyYWN0b3JSZXBvcnQiLCJhZG1pbjpiZWtrYm9rIiwiYWRtaW46YXRsYXMiLCJhZG1pbjpzdGFmZmluZyJdLCJodHRwczovL2FwaS5iZWtrLm5vL2NsYWltcy9lbXBsb3llZUlkIjoxNDM3LCJuYW1lIjoiQmrDuHJuLUl2YXIgU3Ryw7htIiwiZW1haWwiOiJiam9ybi5pdmFyLnN0cm9tQGJla2subm8iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwiaXNzIjoiaHR0cHM6Ly9iZWtrLWRldi5ldS5hdXRoMC5jb20vIiwic3ViIjoid2FhZHxMT0FIbFBSbEJEd2JMdnhSVHdjdVNOY3FfZFkwZlBpNnJaWk1VREJTYTB3IiwiYXVkIjoiUUhReTc1Uzd0bW5oRGRCR1lTbnN6emxoTVB1bDBmQUUiLCJpYXQiOjE2NTU3MDQwOTQsImV4cCI6MTY1NTc0MDA5NH0.WOC8CRBWzMljBZAi7YlPhhQ2SrWWCA4456hDY3l-6i8ErEy_gfivqlpbfnp3BcdHd4tAmU1KMUm97zYuywLrzzW__g5quPJQHbxKsnrPxc0H70O6ly4BXguK0hJGVZ4Jn8CNPHbVckn5aOcNPF8RMZYFx2QhrusEIP8EPQ2Ujs7Zqg8vRZSU0DiU-0gz4l3Ci58I-s7VvCkMsJKP5bH9kt-g9xHTZqUU29h0cfojbq2fpDAiC1t6f1dF7su9pwWY2kZCPBQO6N7d8_H43_FrpZ6dimWzdu36LIx5ZyAZPl_n8eemyChd0MjUd7H0rfPpM_3KaXk-zmZ7Dp43B5mUuw"
  const eventId = "aaab13fe-f7bf-411e-8b65-e6a4e5e3a239"
  let email = `${randomString(10)}@foo.bar` 
  const params = {
    headers: {
      Authorization: `bearer ${token}`,
    }
  }
  const payload = {
    name: randomString(10),
    email: {
      email
    },
    cancelUrlTemplate: `{eventId}{email}cancellationToken`,
  };

  console.log(payload)
  let response = http.post(`http://localhost:5000/events/${eventId}/participants/${email}`, payload, params);
  console.log("Response:", response.status)
  console.log("doo", response.body)
};

export function handleSummary(data) {
  return {
    "Spike-RegisterParticpants.html": htmlReport(data),
  };
}
