const NAME_START = /[A-Za-z_:]/;
const NAME_CONTINUE = /[A-Za-z0-9_.:-]/;

function entityError(value) {
  let offset = 0;
  while (true) {
    const start = value.indexOf("&", offset);
    if (start < 0) return "";
    const end = value.indexOf(";", start + 1);
    if (end < 0) return "unterminated entity reference";
    const entity = value.slice(start, end + 1);
    if (!/^&(amp|lt|gt|quot|apos|#[0-9]+|#x[0-9A-Fa-f]+);$/.test(entity)) {
      return "invalid entity reference " + entity;
    }
    offset = end + 1;
  }
}

export function parseXmlDocument(input) {
  if (typeof input !== "string") {
    return { ok: false, error: "input must be a string" };
  }

  const source = input.startsWith("\uFEFF") ? input.slice(1) : input;
  const stack = [];
  let index = 0;
  let root = null;
  let rootClosed = false;
  let declarationSeen = false;

  function failure(message, at = index) {
    return { ok: false, error: message + " at offset " + at };
  }

  function skipWhitespace() {
    const start = index;
    while (index < source.length && /\s/.test(source[index])) index += 1;
    return index > start;
  }

  function readName() {
    if (index >= source.length || !NAME_START.test(source[index])) return "";
    const start = index;
    index += 1;
    while (index < source.length && NAME_CONTINUE.test(source[index])) index += 1;
    return source.slice(start, index);
  }

  while (index < source.length) {
    if (source[index] !== "<") {
      const next = source.indexOf("<", index);
      const end = next < 0 ? source.length : next;
      const value = source.slice(index, end);
      if (stack.length === 0 && value.trim()) {
        return failure("non-whitespace text is not allowed outside the root element");
      }
      if (stack.length > 0) {
        const problem = entityError(value);
        if (problem) return failure(problem);
      }
      index = end;
      continue;
    }

    if (source.startsWith("<!--", index)) {
      const end = source.indexOf("-->", index + 4);
      if (end < 0) return failure("unclosed XML comment");
      if (source.slice(index + 4, end).includes("--")) {
        return failure("XML comment contains an invalid -- sequence");
      }
      index = end + 3;
      continue;
    }

    if (source.startsWith("<![CDATA[", index)) {
      if (stack.length === 0) return failure("CDATA is not allowed outside the root element");
      const end = source.indexOf("]]>", index + 9);
      if (end < 0) return failure("unclosed CDATA section");
      index = end + 3;
      continue;
    }

    if (source.startsWith("<?", index)) {
      const start = index;
      const end = source.indexOf("?>", index + 2);
      if (end < 0) return failure("unclosed processing instruction");
      index += 2;
      const target = readName();
      if (!target) return failure("processing instruction has no target", start);
      const body = source.slice(index, end);
      if (body.includes("<")) return failure("processing instruction contains an invalid <", start);
      if (target.toLowerCase() === "xml") {
        if (target !== "xml" || declarationSeen || root !== null || stack.length > 0) {
          return failure("XML declaration must appear once before the root element", start);
        }
        declarationSeen = true;
      }
      index = end + 2;
      continue;
    }

    if (source.slice(index, index + 9).toUpperCase() === "<!DOCTYPE") {
      return failure("DOCTYPE is not supported");
    }
    if (source.startsWith("<!", index)) {
      return failure("unsupported or malformed declaration");
    }

    if (source.startsWith("</", index)) {
      const start = index;
      index += 2;
      const name = readName();
      if (!name) return failure("closing tag has no valid name", start);
      skipWhitespace();
      if (source[index] !== ">") return failure("malformed closing tag", start);
      index += 1;
      if (stack.length === 0) return failure("closing tag appears outside the root element", start);
      const expected = stack.at(-1).name;
      if (name !== expected) {
        return failure("mismatched closing tag </" + name + ">; expected </" + expected + ">", start);
      }
      stack.pop();
      if (stack.length === 0) rootClosed = true;
      continue;
    }

    const start = index;
    index += 1;
    const name = readName();
    if (!name) return failure("start tag has no valid name", start);
    if (stack.length === 0 && root !== null) {
      return failure("document contains multiple root elements", start);
    }

    const attributes = new Map();
    let selfClosing = false;
    while (index < source.length) {
      const separated = skipWhitespace();
      if (source.startsWith("/>", index)) {
        selfClosing = true;
        index += 2;
        break;
      }
      if (source[index] === ">") {
        index += 1;
        break;
      }
      if (!separated) return failure("attributes must be separated by whitespace", start);

      const attributeStart = index;
      const attributeName = readName();
      if (!attributeName) return failure("attribute has no valid name", attributeStart);
      if (attributes.has(attributeName)) {
        return failure("duplicate attribute " + attributeName, attributeStart);
      }
      skipWhitespace();
      if (source[index] !== "=") return failure("attribute " + attributeName + " has no =", attributeStart);
      index += 1;
      skipWhitespace();
      const quote = source[index];
      if (quote !== '"' && quote !== "'") {
        return failure("attribute " + attributeName + " must have a quoted value", attributeStart);
      }
      index += 1;
      const valueStart = index;
      const valueEnd = source.indexOf(quote, valueStart);
      if (valueEnd < 0) return failure("attribute " + attributeName + " has an unclosed value", attributeStart);
      const value = source.slice(valueStart, valueEnd);
      if (value.includes("<")) return failure("attribute " + attributeName + " contains an invalid <", attributeStart);
      const problem = entityError(value);
      if (problem) return failure(problem + " in attribute " + attributeName, attributeStart);
      attributes.set(attributeName, value);
      index = valueEnd + 1;
    }

    if (index > source.length || (source[index - 1] !== ">")) {
      return failure("unclosed start tag <" + name + ">", start);
    }

    const element = { name, attributes, selfClosing };
    if (stack.length === 0) root = element;
    if (selfClosing) {
      if (stack.length === 0) rootClosed = true;
    } else {
      stack.push(element);
    }
  }

  if (stack.length > 0) {
    return failure("unclosed element <" + stack.at(-1).name + ">", source.length);
  }
  if (!root) return failure("document has no root element", source.length);
  if (!rootClosed) return failure("root element is not closed", source.length);
  return { ok: true, root };
}
