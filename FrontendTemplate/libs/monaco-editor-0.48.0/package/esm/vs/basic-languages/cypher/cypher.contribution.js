/*!-----------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Version: 0.48.0(0037b13fb5d186fdf1e7df51a9416a2de2b8c670)
 * Released under the MIT license
 * https://github.com/microsoft/monaco-editor/blob/main/LICENSE.txt
 *-----------------------------------------------------------------------------*/


// src/basic-languages/cypher/cypher.contribution.ts
import { registerLanguage } from "../_.contribution.js";
registerLanguage({
  id: "cypher",
  extensions: [".cypher", ".cyp"],
  aliases: ["Cypher", "OpenCypher"],
  loader: () => {
    if (false) {
      return new Promise((resolve, reject) => {
        __require(["vs/basic-languages/cypher/cypher"], resolve, reject);
      });
    } else {
      return import("./cypher.js");
    }
  }
});
