class ChangesHelper {
	static _empty = Symbol("empty");
	static _invalid = Symbol("invalid");
	static _emptyMap = Object.freeze({});

	static _scalarKinds = new Set(["text", "number", "boolean", "enum", "date", "time"]);
	static _fieldOptions = ["from", "at", "when", "compare", "alwaysBuild"];
	static _commonOptions = ["kind", "strict", "isEmpty"];
	static _kindOptions = {
		text: ["trim"],
		number: ["precision", "emptyAs"],
		boolean: [],
		enum: [],
		date: ["unit"],
		time: ["seconds"],
		file: [],
		set: ["items", "multiset"],
		list: ["items", "fields", "split"],
		keyedList: ["key", "savedKey", "items", "fields", "ordered"],
		map: ["values", "keys", "foldKeyCase"],
		object: ["fields", "absent"],
		variant: ["by", "variants"],
		value: ["order", "split"],
		custom: ["equals"],
	};
	static _absentRules = ["empty", "changed", "unchanged"];
	static _dateUnits = { millisecond: 1, second: 1000, minute: 60000 };
	static _numberPattern = /^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$/;
	static _timePattern = /^(\d{1,2}):(\d{2})(?::(\d{2})(?:\.\d+)?)?$/;

	static _definitions = new WeakSet();
	static _warnedPaths = new WeakMap();
	static _auditByDefault = false;

	// Empty values: the one platform rule, per kind

	static _emptyForms = {
		text: (value) => value === null || value === undefined || value === "",
		number: (value) => value === null || value === undefined || (typeof value === "number" && Number.isNaN(value)) || (typeof value === "string" && value.trim() === ""),
		boolean: (value) => value === null || value === undefined,
		enum: (value) => value === null || value === undefined || value === "",
		date: (value) => value === null || value === undefined || value === "",
		time: (value) => value === null || value === undefined || value === "",
		file: (value) => value === null || value === undefined || value === false || value === "" || (this._isListLike(value) && value.length === 0),
		set: (value) => value === null || value === undefined || (Array.isArray(value) && value.length === 0),
		list: (value) => value === null || value === undefined || value === "" || (Array.isArray(value) && value.length === 0),
		keyedList: (value) => value === null || value === undefined || (Array.isArray(value) && value.length === 0),
		map: (value) => value === null || value === undefined || (this._isPlainObject(value) && Object.keys(value).length === 0),
		object: (value) => value === null || value === undefined,
		value: (value) => value === null || value === undefined || value === "" || (typeof value === "number" && Number.isNaN(value)) || (Array.isArray(value) && value.length === 0) || (this._isPlainObject(value) && Object.keys(value).length === 0),
		custom: () => false,
	};

	static _isEmptyFor(rule, value) {
		if (rule.isEmpty !== null && rule.isEmpty(value)) return true;
		return !rule.strict && this._emptyForms[rule.kind](value);
	}

	// Value helpers

	static _isPlainObject(value) {
		if (value === null || typeof value !== "object" || Array.isArray(value)) return false;
		const proto = Object.getPrototypeOf(value);
		return proto === null || Object.getPrototypeOf(proto) === null;
	}

	static _isDate(value) {
		return Object.prototype.toString.call(value) === "[object Date]";
	}

	static _isListLike(value) {
		return value !== null && typeof value === "object" && typeof value.length === "number";
	}

	static _isNullish(value) {
		return value === null || value === undefined;
	}

	static _hasOwn(holder, key) {
		return Object.prototype.hasOwnProperty.call(holder, key);
	}

	static _readValue(holder, key) {
		if (holder === null || holder === undefined) return undefined;
		const value = holder[key];
		if (typeof value === "function" && this._isPlainObject(holder)) return value();
		return value;
	}

	static _resolvePath(holder, segments, callReaders) {
		let value = holder;
		for (const segment of segments) {
			if (value === null || value === undefined) return undefined;
			value = callReaders ? this._readValue(value, segment) : value[segment];
		}
		return value;
	}

	static _hasPath(holder, segments) {
		let value = holder;
		for (let index = 0; index < segments.length; index++) {
			if (value === null || typeof value !== "object") return false;
			if (!(segments[index] in value)) return false;
			value = value[segments[index]];
		}
		return true;
	}

	static _joinPath(path, name) {
		return path === "" ? String(name) : `${path}.${name}`;
	}

	static _keyText(key) {
		return this._isNullish(key) ? "\u0000" : String(key);
	}

	static _splitText(text, separator) {
		return text.split(separator).map((part) => part.trim()).filter((part) => part !== "");
	}

	static _toNumber(value) {
		if (typeof value === "number") return value;
		if (typeof value === "string") {
			const trimmed = value.trim();
			if (this._numberPattern.test(trimmed)) return Number(trimmed);
		}
		return value;
	}

	static _roundTo(value, precision) {
		if (precision === null || typeof value !== "number" || !Number.isFinite(value)) return value;
		const text = String(value);
		if (text.includes("e")) return Number(value.toFixed(precision));
		return Number(`${Math.round(Number(`${text}e${precision}`))}e-${precision}`);
	}

	// Scalar kinds

	// One comparable form per scalar kind
	static _canonical(rule, value) {
		let current = value;
		if (rule.trim && typeof current === "string") current = current.trim();
		if (this._isEmptyFor(rule, current)) {
			if (rule.kind === "boolean") return false;
			if (rule.kind === "number" && rule.emptyAs !== null) return this._roundTo(rule.emptyAs, rule.precision);
			return this._empty;
		}
		if (current === null || current === undefined) return current;
		switch (rule.kind) {
			case "text":
				return typeof current === "number" || typeof current === "boolean" ? String(current) : current;
			case "number":
				return this._roundTo(this._toNumber(current), rule.precision);
			case "boolean":
				if (typeof current === "string") {
					const lowered = current.trim().toLowerCase();
					if (lowered === "true") return true;
					if (lowered === "false") return false;
				}
				return current;
			case "enum":
				if (typeof current === "object" && !Array.isArray(current) && "value" in current) return this._canonical(rule, current.value);
				return this._toNumber(current);
			case "date": {
				let time = current;
				if (this._isDate(current)) time = current.getTime();
				else if (typeof current === "string") {
					const parsed = Date.parse(current);
					if (!Number.isNaN(parsed)) time = parsed;
				}
				return typeof time === "number" ? Math.floor(time / rule.unit) : time;
			}
			case "time": {
				if (typeof current !== "string") return current;
				const match = this._timePattern.exec(current.trim());
				if (match === null) return current;
				return Number(match[1]) * 3600 + Number(match[2]) * 60 + (rule.seconds && match[3] !== undefined ? Number(match[3]) : 0);
			}
		}
		return current;
	}

	static _sameScalar(left, right) {
		return left === right || (left !== left && right !== right);
	}

	static _getterValue(rule, value) {
		if (!this._scalarKinds.has(rule.kind)) return value;
		const result = this._canonical(rule, value);
		return result === this._empty ? null : result;
	}

	// Walk state

	static _createState(options, root) {
		const audit = options.audit === undefined ? this._auditByDefault : options.audit;
		return {
			build: options.build,
			paths: options.paths,
			context: options.context,
			audit,
			trackPath: options.paths || audit,
			root,
			changed: false,
			differences: [],
			missing: [],
			missingSet: audit ? new Set() : null,
		};
	}

	static _canCompare(state) {
		return !state.changed || state.paths;
	}

	static _halted(state) {
		return state.changed && !state.paths && !state.build;
	}

	static _record(state, path, type, saved, current) {
		state.changed = true;
		if (state.paths) state.differences.push({ path, type, saved, current });
	}

	static _noteMissing(state, path) {
		if (state.missingSet.has(path)) return;
		state.missingSet.add(path);
		state.missing.push(path);
		let warned = this._warnedPaths.get(state.root);
		if (warned === undefined) {
			warned = new Set();
			this._warnedPaths.set(state.root, warned);
		}
		if (!warned.has(path)) {
			warned.add(path);
			console.warn(`${this.name}: the saved side has no key for "${path}"`);
		}
	}

	// Reading the two sides

	static _readCurrent(scope, name, rule) {
		if (scope.cache !== null) {
			const cached = scope.cache.get(name);
			if (cached !== undefined && cached.rule === rule) return cached.value;
		}
		let value;
		if (scope.current === undefined) value = undefined;
		else if (rule.atFn !== null) value = rule.atFn(scope.current, scope.state.context);
		else if (rule.atPath !== null) value = this._resolvePath(scope.current, rule.atPath, true);
		else value = this._readValue(scope.current, name);
		if (scope.cache !== null) scope.cache.set(name, { rule, value });
		return value;
	}

	static _readSaved(scope, name, rule) {
		const holder = scope.saved;
		if (holder === undefined) return undefined;
		const state = scope.state;
		if (state.audit && rule.fromFn === null && holder !== null && typeof holder === "object") {
			if (!this._hasPath(holder, rule.fromPath !== null ? rule.fromPath : [name])) this._noteMissing(state, this._joinPath(scope.path, name));
		}
		if (rule.fromFn !== null) return rule.fromFn(holder, state.context);
		if (rule.fromPath !== null) return this._resolvePath(holder, rule.fromPath, false);
		return holder[name];
	}

	// Field reader for when and by, looks in enclosing objects too
	static _getterFor(scope, side) {
		const slot = side === "saved" ? "savedGetter" : "currentGetter";
		if (scope[slot] !== null) return scope[slot];
		const getter = (name) => {
			let target = scope;
			let fieldName = String(name);
			while (fieldName.startsWith("../")) {
				target = target.parent;
				fieldName = fieldName.slice(3);
				if (target === null) throw new Error(`${this.name}: "${name}" goes above the outermost object`);
			}
			while (target !== null) {
				const fieldRule = target.rule.fieldMap.get(fieldName);
				if (fieldRule !== undefined) {
					const value = side === "saved" ? this._readSaved(target, fieldName, fieldRule) : this._readCurrent(target, fieldName, fieldRule);
					return this._getterValue(fieldRule, value);
				}
				target = target.parent;
			}
			throw new Error(`${this.name}: "${name}" is not a field of this object or of an enclosing one`);
		};
		scope[slot] = getter;
		return getter;
	}

	static _variantRule(rule, scope, side) {
		const key = rule.by(this._getterFor(scope, side), scope.state.context);
		if (this._isNullish(key)) return undefined;
		return rule.variants.get(String(key));
	}

	// Walk

	// Hands each rule to its kind
	static _walk(rule, saved, current, path, state, scope, comparing) {
		switch (rule.kind) {
			case "object":
				return this._walkObject(rule, saved, current, path, state, scope, comparing);
			case "list":
				return this._walkList(rule, saved, current, path, state, scope, comparing);
			case "keyedList":
				return this._walkKeyedList(rule, saved, current, path, state, scope, comparing);
			case "map":
				return this._walkMap(rule, saved, current, path, state, scope, comparing);
			case "set":
				return this._walkSet(rule, saved, current, path, state, comparing);
			case "value":
				if (comparing && this._canCompare(state)) this._walkValueTree(rule, saved, current, path, state);
				return state.build ? current : undefined;
			case "file":
				if (comparing && this._canCompare(state) && !this._sameFiles(rule, saved, current)) this._record(state, path, "changed", saved, current);
				return state.build ? current : undefined;
			case "custom":
				if (comparing && this._canCompare(state) && rule.equals(saved, current, state.context) !== true) this._record(state, path, "changed", saved, current);
				return state.build ? current : undefined;
			default:
				if (comparing && this._canCompare(state) && !this._sameScalar(this._canonical(rule, saved), this._canonical(rule, current))) this._record(state, path, "changed", saved, current);
				return state.build ? current : undefined;
		}
	}

	// Field against field, with when and variants
	static _walkObject(rule, saved, current, path, state, parentScope, comparing) {
		const savedAbsent = this._isNullish(saved);
		const currentAbsent = this._isNullish(current);
		if (savedAbsent && currentAbsent) return state.build ? current : undefined;
		let compareFields = comparing;
		if (compareFields && savedAbsent !== currentAbsent && rule.absent !== "empty") {
			if (rule.absent === "changed" && this._canCompare(state)) this._record(state, path, savedAbsent ? "added" : "removed", saved, current);
			compareFields = false;
		}
		const out = state.build && !currentAbsent ? {} : undefined;
		if (!compareFields && out === undefined) return state.build ? current : undefined;
		const scope = {
			rule,
			saved: savedAbsent ? undefined : saved,
			current: currentAbsent ? undefined : current,
			cache: rule.needsCache ? new Map() : null,
			parent: parentScope,
			path,
			state,
			savedGetter: null,
			currentGetter: null,
		};
		for (const [name, fieldRule] of rule.fieldList) {
			if (this._halted(state)) break;
			let compareField = compareFields && fieldRule.compare && this._canCompare(state);
			if (!compareField && out === undefined) continue;
			const fieldPath = state.trackPath ? this._joinPath(path, name) : "";
			if (fieldRule.when !== null) {
				const currentActive = Boolean(fieldRule.when(this._getterFor(scope, "current"), state.context));
				const savedActive = compareField ? Boolean(fieldRule.when(this._getterFor(scope, "saved"), state.context)) : currentActive;
				if (currentActive !== savedActive) {
					this._record(state, fieldPath, currentActive ? "added" : "removed", undefined, undefined);
					compareField = false;
				}
				if (!currentActive) {
					if (out !== undefined && fieldRule.alwaysBuild) out[name] = this._walk(fieldRule, undefined, this._readCurrent(scope, name, fieldRule), fieldPath, state, scope, false);
					continue;
				}
			}
			let effective = fieldRule;
			if (fieldRule.kind === "variant") {
				const currentVariant = this._variantRule(fieldRule, scope, "current");
				const savedVariant = compareField ? this._variantRule(fieldRule, scope, "saved") : currentVariant;
				if (currentVariant !== savedVariant) {
					this._record(state, fieldPath, savedVariant === undefined ? "added" : currentVariant === undefined ? "removed" : "changed", undefined, undefined);
					compareField = false;
				}
				if (currentVariant === undefined) continue;
				effective = currentVariant;
			}
			if (!compareField) {
				if (out !== undefined) out[name] = this._walk(effective, undefined, this._readCurrent(scope, name, effective), fieldPath, state, scope, false);
				continue;
			}
			const built = this._walk(effective, this._readSaved(scope, name, effective), this._readCurrent(scope, name, effective), fieldPath, state, scope, true);
			if (out !== undefined) out[name] = built;
		}
		return state.build ? (currentAbsent ? current : out) : undefined;
	}

	// Lists, sets and maps

	static _toList(rule, value) {
		if (rule.isEmpty !== null && rule.isEmpty(value)) return [];
		if (this._isNullish(value) || value === "") return [];
		if (Array.isArray(value)) return value;
		if (typeof value === "string" && rule.split !== null) return this._splitText(value, rule.split);
		return this._invalid;
	}

	static _strictNullDiffers(rule, saved, current) {
		return rule.strict && this._isNullish(saved) !== this._isNullish(current);
	}

	// Items by position
	static _walkList(rule, saved, current, path, state, scope, comparing) {
		const currentItems = this._toList(rule, current);
		let compareItems = comparing && this._canCompare(state);
		let savedItems = [];
		if (compareItems) {
			savedItems = this._toList(rule, saved);
			if (savedItems === this._invalid || currentItems === this._invalid || this._strictNullDiffers(rule, saved, current) || (!state.paths && savedItems.length !== currentItems.length)) {
				this._record(state, path, "changed", saved, current);
				compareItems = false;
			}
		}
		if (!state.build && !compareItems) return undefined;
		if (currentItems === this._invalid) return state.build ? current : undefined;
		const out = state.build && Array.isArray(current) ? [] : undefined;
		const count = compareItems ? Math.max(savedItems.length, currentItems.length) : currentItems.length;
		for (let index = 0; index < count; index++) {
			if (this._halted(state)) break;
			const compareItem = compareItems && this._canCompare(state);
			if (!compareItem && out === undefined) break;
			const itemPath = state.trackPath ? `${path}[${index}]` : "";
			if (index >= currentItems.length) {
				if (compareItem) this._record(state, itemPath, "removed", savedItems[index], undefined);
				continue;
			}
			if (compareItem && index >= savedItems.length) {
				this._record(state, itemPath, "added", undefined, currentItems[index]);
				if (out !== undefined) out.push(this._walk(rule.items, undefined, currentItems[index], itemPath, state, scope, false));
				continue;
			}
			const built = this._walk(rule.items, compareItem ? savedItems[index] : undefined, currentItems[index], itemPath, state, scope, compareItem);
			if (out !== undefined) out.push(built);
		}
		return state.build && out === undefined ? current : out;
	}

	// Items matched by key: added, removed, reordered
	static _walkKeyedList(rule, saved, current, path, state, scope, comparing) {
		const currentItems = this._toList(rule, current);
		let compareItems = comparing && this._canCompare(state);
		let savedItems = [];
		if (compareItems) {
			savedItems = this._toList(rule, saved);
			if (savedItems === this._invalid || currentItems === this._invalid || this._strictNullDiffers(rule, saved, current)) {
				this._record(state, path, "changed", saved, current);
				compareItems = false;
			} else if (!state.paths && !state.build && savedItems.length !== currentItems.length) {
				this._record(state, path, "changed", saved, current);
				return undefined;
			}
		}
		if (!state.build && !compareItems) return undefined;
		if (currentItems === this._invalid) return state.build ? current : undefined;
		let savedIndex = null;
		let matched = null;
		if (compareItems) {
			savedIndex = new Map();
			for (let index = 0; index < savedItems.length; index++) {
				const key = this._keyText(rule.savedKeyOf(savedItems[index]));
				if (!savedIndex.has(key)) savedIndex.set(key, index);
			}
			matched = new Uint8Array(savedItems.length);
		}
		const out = state.build && Array.isArray(current) ? [] : undefined;
		let previousPosition = -1;
		let reordered = false;
		for (const item of currentItems) {
			if (this._halted(state)) break;
			const compareItem = compareItems && this._canCompare(state);
			if (!compareItem && out === undefined) break;
			const key = this._keyText(rule.keyOf(item));
			const itemPath = state.trackPath ? `${path}[${key}]` : "";
			if (!compareItem) {
				out.push(this._walk(rule.items, undefined, item, itemPath, state, scope, false));
				continue;
			}
			const position = savedIndex.get(key);
			if (position === undefined || matched[position] === 1) {
				this._record(state, itemPath, "added", undefined, item);
				if (out !== undefined) out.push(this._walk(rule.items, undefined, item, itemPath, state, scope, false));
				continue;
			}
			matched[position] = 1;
			if (position < previousPosition) reordered = true;
			previousPosition = position;
			const built = this._walk(rule.items, savedItems[position], item, itemPath, state, scope, true);
			if (out !== undefined) out.push(built);
		}
		if (compareItems && this._canCompare(state) && !this._halted(state)) {
			for (let index = 0; index < savedItems.length; index++) {
				if (matched[index] === 1) continue;
				this._record(state, state.trackPath ? `${path}[${this._keyText(rule.savedKeyOf(savedItems[index]))}]` : "", "removed", savedItems[index], undefined);
				if (!state.paths) break;
			}
			if (rule.ordered && reordered && this._canCompare(state)) this._record(state, path, "reordered", saved, current);
		}
		return state.build && out === undefined ? current : out;
	}

	static _toMap(rule, value) {
		if (rule.isEmpty !== null && rule.isEmpty(value)) return this._emptyMap;
		if (this._isNullish(value)) return this._emptyMap;
		if (typeof value === "object" && !Array.isArray(value)) return value;
		return this._invalid;
	}

	static _foldIndex(map) {
		const index = new Map();
		for (const key of Object.keys(map)) {
			const folded = key.toLowerCase();
			if (!index.has(folded)) index.set(folded, key);
		}
		return index;
	}

	static _mapKeys(rule, savedMap, currentMap, savedIndex, currentIndex, compareEntries, state) {
		if (rule.keys !== null) {
			const listed = typeof rule.keys === "function" ? rule.keys(state.context) : rule.keys;
			if (!Array.isArray(listed)) throw new Error(`${this.name}: the keys of a map must be an array`);
			return rule.foldKeyCase ? listed.map((key) => String(key).toLowerCase()) : listed.map(String);
		}
		const keys = currentIndex !== null ? [...currentIndex.keys()] : Object.keys(currentMap);
		if (compareEntries) {
			const seen = new Set(keys);
			for (const key of savedIndex !== null ? savedIndex.keys() : Object.keys(savedMap)) {
				if (!seen.has(key)) keys.push(key);
			}
		}
		return keys;
	}

	// Entries by key, key order ignored
	static _walkMap(rule, saved, current, path, state, scope, comparing) {
		const currentMap = this._toMap(rule, current);
		let compareEntries = comparing && this._canCompare(state);
		let savedMap = this._emptyMap;
		if (compareEntries) {
			savedMap = this._toMap(rule, saved);
			if (savedMap === this._invalid || currentMap === this._invalid || this._strictNullDiffers(rule, saved, current)) {
				this._record(state, path, "changed", saved, current);
				compareEntries = false;
			}
		}
		if (!state.build && !compareEntries) return undefined;
		if (currentMap === this._invalid) return state.build ? current : undefined;
		const currentIndex = rule.foldKeyCase ? this._foldIndex(currentMap) : null;
		const savedIndex = compareEntries && rule.foldKeyCase ? this._foldIndex(savedMap) : null;
		const keys = this._mapKeys(rule, savedMap, currentMap, savedIndex, currentIndex, compareEntries, state);
		const out = state.build && (currentMap === current || rule.keys !== null) ? {} : undefined;
		for (const key of keys) {
			if (this._halted(state)) break;
			const compareEntry = compareEntries && this._canCompare(state);
			if (!compareEntry && out === undefined) break;
			const currentKey = currentIndex !== null ? currentIndex.get(key) : key;
			const currentHas = currentKey !== undefined && this._hasOwn(currentMap, currentKey);
			if (!currentHas && !compareEntry && rule.keys === null) continue;
			const entryPath = state.trackPath ? this._joinPath(path, key) : "";
			const currentValue = currentHas ? this._readValue(currentMap, currentKey) : undefined;
			let built;
			if (compareEntry) {
				const savedKey = savedIndex !== null ? savedIndex.get(key) : key;
				const savedValue = savedKey !== undefined && this._hasOwn(savedMap, savedKey) ? savedMap[savedKey] : undefined;
				built = this._walk(rule.values, savedValue, currentValue, entryPath, state, scope, true);
			} else {
				built = this._walk(rule.values, undefined, currentValue, entryPath, state, scope, false);
			}
			if (out !== undefined && (currentHas || rule.keys !== null)) out[currentHas ? currentKey : key] = built;
		}
		return state.build && out === undefined ? current : out;
	}

	static _setKey(itemRule, value) {
		const result = this._canonical(itemRule, value);
		if (result === this._empty) return "empty:";
		return `${typeof result}:${String(result)}`;
	}

	static _walkSet(rule, saved, current, path, state, comparing) {
		if (comparing && this._canCompare(state)) {
			const savedItems = this._toList(rule, saved);
			const currentItems = this._toList(rule, current);
			if (savedItems === this._invalid || currentItems === this._invalid || this._strictNullDiffers(rule, saved, current)) this._record(state, path, "changed", saved, current);
			else this._compareSetItems(rule, savedItems, currentItems, path, state);
		}
		return state.build ? (Array.isArray(current) ? current.slice() : current) : undefined;
	}

	// Order ignored, duplicates counted only for a multiset
	static _compareSetItems(rule, savedItems, currentItems, path, state) {
		const itemPath = (key) => (state.trackPath ? `${path}[${key.slice(key.indexOf(":") + 1)}]` : "");
		const counts = new Map();
		for (const item of savedItems) {
			const key = this._setKey(rule.items, item);
			counts.set(key, rule.multiset ? (counts.get(key) || 0) + 1 : 1);
		}
		const seen = new Set();
		for (const item of currentItems) {
			const key = this._setKey(rule.items, item);
			if (rule.multiset) {
				const left = counts.get(key) || 0;
				if (left === 0) {
					this._record(state, itemPath(key), "added", undefined, item);
					if (!state.paths) return;
					continue;
				}
				counts.set(key, left - 1);
				continue;
			}
			if (seen.has(key)) continue;
			seen.add(key);
			if (!counts.has(key)) {
				this._record(state, itemPath(key), "added", undefined, item);
				if (!state.paths) return;
			}
		}
		for (const [key, left] of counts) {
			if (rule.multiset ? left === 0 : seen.has(key)) continue;
			this._record(state, itemPath(key), "removed", key, undefined);
			if (!state.paths) return;
		}
	}

	// Free-shape values

	static _isEnumObject(value) {
		if (!this._isPlainObject(value) || !this._hasOwn(value, "value")) return false;
		const keys = Object.keys(value);
		if (keys.length > 2 || (keys.length === 2 && !this._hasOwn(value, "name"))) return false;
		return typeof value.value === "number" || typeof value.value === "string";
	}

	static _valueCanonical(rule, value) {
		if (rule.isEmpty !== null && rule.isEmpty(value)) return this._empty;
		if (!rule.strict && this._emptyForms.value(value)) return this._empty;
		if (this._isDate(value)) return value.getTime();
		if (this._isEnumObject(value)) return this._valueCanonical(rule, value.value);
		return value;
	}

	static _looseEqual(left, right) {
		if (this._sameScalar(left, right)) return true;
		if (typeof left === "number" && typeof right === "string") return this._sameScalar(left, this._toNumber(right));
		if (typeof left === "string" && typeof right === "number") return this._sameScalar(this._toNumber(left), right);
		if (typeof left === "boolean" && typeof right === "string") return right.trim().toLowerCase() === String(left);
		if (typeof left === "string" && typeof right === "boolean") return left.trim().toLowerCase() === String(right);
		return false;
	}

	static _looseKey(value) {
		if (value === this._empty) return "empty:";
		if (typeof value === "string") {
			const number = this._toNumber(value);
			if (typeof number === "number") return `number:${number}`;
			const lowered = value.trim().toLowerCase();
			if (lowered === "true" || lowered === "false") return `boolean:${lowered}`;
		}
		return `${typeof value}:${String(value)}`;
	}

	static _isScalarValue(value) {
		return value === null || typeof value !== "object" || this._isDate(value) || this._isEnumObject(value);
	}

	// Free shape, compared deeply
	static _walkValueTree(rule, saved, current, path, state) {
		let left = this._valueCanonical(rule, saved);
		let right = this._valueCanonical(rule, current);
		if (left === this._empty && right === this._empty) return;
		if (left === this._empty && this._isPlainObject(right)) left = this._emptyMap;
		else if (right === this._empty && this._isPlainObject(left)) right = this._emptyMap;
		if (rule.split !== null) {
			if (Array.isArray(left) && typeof right === "string") right = this._splitText(right, rule.split);
			else if (typeof left === "string" && Array.isArray(right)) left = this._splitText(left, rule.split);
		}
		if (Array.isArray(left) && Array.isArray(right)) {
			this._compareValueArrays(rule, left, right, path, state);
			return;
		}
		if (this._isPlainObject(left) && this._isPlainObject(right)) {
			for (const key of Object.keys(right)) {
				if (!this._canCompare(state)) return;
				this._walkValueTree(rule, left[key], right[key], state.trackPath ? this._joinPath(path, key) : "", state);
			}
			for (const key of Object.keys(left)) {
				if (!this._canCompare(state)) return;
				if (this._hasOwn(right, key)) continue;
				this._walkValueTree(rule, left[key], undefined, state.trackPath ? this._joinPath(path, key) : "", state);
			}
			return;
		}
		if (left === this._empty || right === this._empty || !this._looseEqual(left, right)) this._record(state, path, "changed", saved, current);
	}

	// Free-shape arrays, as a set or by position
	static _compareValueArrays(rule, left, right, path, state) {
		if (rule.order === "set" && left.every((item) => this._isScalarValue(item)) && right.every((item) => this._isScalarValue(item))) {
			const leftKeys = new Set(left.map((item) => this._looseKey(this._valueCanonical(rule, item))));
			const rightKeys = new Set(right.map((item) => this._looseKey(this._valueCanonical(rule, item))));
			let same = leftKeys.size === rightKeys.size;
			if (same) {
				for (const key of rightKeys) {
					if (!leftKeys.has(key)) {
						same = false;
						break;
					}
				}
			}
			if (!same) this._record(state, path, "changed", left, right);
			return;
		}
		if (left.length !== right.length && !state.paths) {
			this._record(state, path, "changed", left, right);
			return;
		}
		const count = Math.max(left.length, right.length);
		for (let index = 0; index < count; index++) {
			if (!this._canCompare(state)) return;
			this._walkValueTree(rule, left[index], right[index], state.trackPath ? `${path}[${index}]` : "", state);
		}
	}

	// Files

	static _toFiles(rule, value) {
		if (this._isEmptyFor(rule, value)) return [];
		if (Array.isArray(value)) return value;
		if (this._isListLike(value) && typeof value !== "string") return Array.from(value);
		return [value];
	}

	static _sameFiles(rule, saved, current) {
		const savedFiles = this._toFiles(rule, saved);
		const currentFiles = this._toFiles(rule, current);
		if (savedFiles.length !== currentFiles.length) return false;
		for (let index = 0; index < savedFiles.length; index++) {
			if (savedFiles[index] !== currentFiles[index]) return false;
		}
		return true;
	}

	// Definitions

	static _fail(path, message) {
		throw new Error(`${this.name}: ${message} (at "${path === "" ? "root" : path}")`);
	}

	static _readBoolean(source, option, path, fallback) {
		if (source[option] === undefined) return fallback;
		if (typeof source[option] !== "boolean") this._fail(path, `"${option}" must be true or false`);
		return source[option];
	}

	static _readFunction(source, option, path) {
		if (source[option] === undefined) return null;
		if (typeof source[option] !== "function") this._fail(path, `"${option}" must be a function`);
		return source[option];
	}

	static _readLocation(source, option, path) {
		const value = source[option];
		if (value === undefined) return { fn: null, segments: null };
		if (typeof value === "function") return { fn: value, segments: null };
		if (typeof value === "string" && value !== "") return { fn: null, segments: value.split(".") };
		this._fail(path, `"${option}" must be a non-empty path text or a function`);
	}

	static _readFields(source, path) {
		if (!this._isPlainObject(source.fields) || Object.keys(source.fields).length === 0) this._fail(path, "an object needs at least one field");
		const fieldList = [];
		const fieldMap = new Map();
		for (const [name, spec] of Object.entries(source.fields)) {
			const fieldRule = this._compileRule(spec, this._joinPath(path, name), true);
			fieldList.push([name, fieldRule]);
			fieldMap.set(name, fieldRule);
		}
		return { fieldList, fieldMap };
	}

	static _readItems(source, path, defaultKind) {
		if (source.items !== undefined && source.fields !== undefined) this._fail(path, "give either items or fields, not both");
		if (source.fields !== undefined) return this._compileRule({ kind: "object", fields: source.fields }, `${path}[]`, false);
		return this._compileRule(source.items === undefined ? defaultKind : source.items, `${path}[]`, false);
	}

	static _readSplit(source, path) {
		if (source.split === undefined) return null;
		if (typeof source.split !== "string" || source.split === "") this._fail(path, "\"split\" must be a separator text");
		return source.split;
	}

	static _readKey(value, path, option) {
		if (typeof value === "function") return value;
		if (typeof value === "string" && value !== "") return (item) => this._readValue(item, value);
		this._fail(path, `"${option}" must be a field name or a function`);
	}

	// Checks a rule once and freezes it; mistakes throw here
	static _compileRule(spec, path, isField) {
		const source = typeof spec === "string" ? { kind: spec } : spec;
		if (!this._isPlainObject(source)) this._fail(path, "a rule must be a kind name or an object with a kind");
		const kind = source.kind;
		if (typeof kind !== "string" || !this._hasOwn(this._kindOptions, kind)) this._fail(path, `unknown kind "${String(kind)}"`);
		for (const option of Object.keys(source)) {
			if (this._commonOptions.includes(option) || this._kindOptions[kind].includes(option)) continue;
			if (this._fieldOptions.includes(option)) {
				if (!isField) this._fail(path, `"${option}" only works on a field of an object`);
				continue;
			}
			this._fail(path, `option "${option}" is not known for kind "${kind}"`);
		}
		const from = this._readLocation(source, "from", path);
		const at = this._readLocation(source, "at", path);
		const rule = {
			kind,
			fromFn: from.fn,
			fromPath: from.segments,
			atFn: at.fn,
			atPath: at.segments,
			when: this._readFunction(source, "when", path),
			compare: this._readBoolean(source, "compare", path, true),
			alwaysBuild: this._readBoolean(source, "alwaysBuild", path, false),
			strict: this._readBoolean(source, "strict", path, false),
			isEmpty: this._readFunction(source, "isEmpty", path),
			trim: false,
			precision: null,
			emptyAs: null,
			unit: 1,
			seconds: false,
			items: null,
			multiset: false,
			split: null,
			keyOf: null,
			savedKeyOf: null,
			ordered: false,
			values: null,
			keys: null,
			foldKeyCase: false,
			fieldList: null,
			fieldMap: null,
			absent: "empty",
			by: null,
			variants: null,
			order: "ordered",
			equals: null,
			needsCache: false,
			hasWhen: false,
		};
		if (rule.alwaysBuild && rule.when === null) this._fail(path, "\"alwaysBuild\" needs \"when\"");
		switch (kind) {
			case "text":
				rule.trim = this._readBoolean(source, "trim", path, false);
				break;
			case "number":
				if (source.precision !== undefined) {
					if (!Number.isInteger(source.precision) || source.precision < 0 || source.precision > 15) this._fail(path, "\"precision\" must be a whole number from 0 to 15");
					rule.precision = source.precision;
				}
				if (source.emptyAs !== undefined) {
					if (typeof source.emptyAs !== "number" || !Number.isFinite(source.emptyAs)) this._fail(path, "\"emptyAs\" must be a number");
					rule.emptyAs = source.emptyAs;
				}
				break;
			case "date":
				if (source.unit !== undefined) {
					if (!this._hasOwn(this._dateUnits, source.unit)) this._fail(path, `"unit" must be one of ${Object.keys(this._dateUnits).join(", ")}`);
					rule.unit = this._dateUnits[source.unit];
				}
				break;
			case "time":
				rule.seconds = this._readBoolean(source, "seconds", path, false);
				break;
			case "set":
				rule.items = this._compileRule(source.items === undefined ? "text" : source.items, `${path}[]`, false);
				if (!this._scalarKinds.has(rule.items.kind)) this._fail(path, "the items of a set must be a scalar kind (text, number, boolean, enum, date, time)");
				rule.multiset = this._readBoolean(source, "multiset", path, false);
				break;
			case "list":
				rule.items = this._readItems(source, path, "value");
				rule.split = this._readSplit(source, path);
				break;
			case "keyedList":
				if (source.key === undefined) this._fail(path, "a keyed list needs a key");
				rule.keyOf = this._readKey(source.key, path, "key");
				rule.savedKeyOf = source.savedKey === undefined ? (typeof source.key === "string" ? (item) => (this._isNullish(item) ? undefined : item[source.key]) : rule.keyOf) : this._readKey(source.savedKey, path, "savedKey");
				if (source.items === undefined && source.fields === undefined) this._fail(path, "a keyed list needs items or fields");
				rule.items = this._readItems(source, path, "object");
				if (!["object", "value", "custom"].includes(rule.items.kind)) this._fail(path, "the items of a keyed list must be an object, value or custom rule");
				rule.ordered = this._readBoolean(source, "ordered", path, false);
				break;
			case "map":
				rule.values = this._compileRule(source.values === undefined ? "value" : source.values, this._joinPath(path, "*"), false);
				if (source.keys !== undefined) {
					if (!Array.isArray(source.keys) && typeof source.keys !== "function") this._fail(path, "\"keys\" must be an array or a function");
					rule.keys = Array.isArray(source.keys) ? Object.freeze(source.keys.map(String)) : source.keys;
				}
				rule.foldKeyCase = this._readBoolean(source, "foldKeyCase", path, false);
				break;
			case "object": {
				if (source.strict !== undefined) this._fail(path, "an object does not take \"strict\", use \"absent\"");
				const fields = this._readFields(source, path);
				rule.fieldList = fields.fieldList;
				rule.fieldMap = fields.fieldMap;
				if (source.absent !== undefined) {
					if (!this._absentRules.includes(source.absent)) this._fail(path, `"absent" must be one of ${this._absentRules.join(", ")}`);
					rule.absent = source.absent;
				}
				rule.needsCache = rule.fieldList.some(([, fieldRule]) => fieldRule.hasWhen);
				break;
			}
			case "variant": {
				if (!isField) this._fail(path, "a variant only works on a field of an object");
				rule.by = this._readFunction(source, "by", path);
				if (rule.by === null) this._fail(path, "a variant needs \"by\"");
				if (!this._isPlainObject(source.variants) || Object.keys(source.variants).length === 0) this._fail(path, "a variant needs at least one entry in \"variants\"");
				rule.variants = new Map();
				for (const [key, variantSpec] of Object.entries(source.variants)) {
					const variantSource = typeof variantSpec === "string" ? { kind: variantSpec } : variantSpec;
					if (this._isPlainObject(variantSource) && (variantSource.when !== undefined || variantSource.kind === "variant")) this._fail(this._joinPath(path, key), "a variant entry takes no \"when\" and is not itself a variant");
					const compiled = this._compileRule(variantSource, `${path}<${key}>`, true);
					const located = compiled.fromFn !== null || compiled.fromPath !== null || compiled.atFn !== null || compiled.atPath !== null ? compiled : Object.freeze({ ...compiled, fromFn: rule.fromFn, fromPath: rule.fromPath, atFn: rule.atFn, atPath: rule.atPath });
					rule.variants.set(key, located);
				}
				rule.hasWhen = true;
				break;
			}
			case "value":
				if (source.order !== undefined) {
					if (source.order !== "ordered" && source.order !== "set") this._fail(path, "\"order\" must be ordered or set");
					rule.order = source.order;
				}
				rule.split = this._readSplit(source, path);
				break;
			case "custom":
				rule.equals = this._readFunction(source, "equals", path);
				if (rule.equals === null) this._fail(path, "a custom rule needs \"equals\"");
				break;
		}
		rule.hasWhen = rule.hasWhen || rule.when !== null || rule.needsCache || (rule.items !== null && rule.items.hasWhen) || (rule.values !== null && rule.values.hasWhen);
		return Object.freeze(rule);
	}

	static _readOptions(options) {
		if (options === undefined) options = {};
		if (!this._isPlainObject(options)) throw new Error(`${this.name}: options must be an object`);
		for (const option of Object.keys(options)) {
			if (!["build", "paths", "context", "audit"].includes(option)) throw new Error(`${this.name}: unknown compare option "${option}"`);
		}
		for (const option of ["build", "paths", "audit"]) {
			if (options[option] !== undefined && typeof options[option] !== "boolean") throw new Error(`${this.name}: "${option}" must be true or false`);
		}
		return { build: options.build === true, paths: options.paths === true, audit: options.audit, context: options.context };
	}

	// One compare: walk both sides, shape the result
	static _runCompare(root, saved, current, options) {
		const state = this._createState(this._readOptions(options), root);
		const built = this._walk(root, saved, this._isNullish(current) ? {} : current, "", state, null, true);
		const result = { hasChanges: state.changed };
		if (state.build) result.changes = built;
		if (state.paths) {
			result.paths = state.differences.map((difference) => difference.path);
			result.differences = state.differences;
		}
		if (state.audit) result.missingSavedKeys = state.missing;
		return result;
	}

	static define(fields) {
		if (!ChangesHelper._isPlainObject(fields)) ChangesHelper._fail("", "define() needs an object of fields");
		const root = ChangesHelper._compileRule({ kind: "object", fields }, "", false);
		const definition = Object.freeze({
			compare: (saved, current, options) => ChangesHelper._runCompare(root, saved, current, options),
		});
		ChangesHelper._definitions.add(definition);
		return definition;
	}

	// Sections

	static defineSections(sections) {
		if (!ChangesHelper._isPlainObject(sections) || Object.keys(sections).length === 0) ChangesHelper._fail("", "defineSections() needs an object of sections");
		const list = [];
		for (const [name, section] of Object.entries(sections)) {
			if (!ChangesHelper._isPlainObject(section)) ChangesHelper._fail(name, "a section must be an object");
			for (const option of Object.keys(section)) {
				if (!["definition", "saved", "current", "check"].includes(option)) ChangesHelper._fail(name, `option "${option}" is not known for a section`);
			}
			if (section.check !== undefined) {
				if (typeof section.check !== "function") ChangesHelper._fail(name, "\"check\" must be a function");
				if (section.definition !== undefined || section.saved !== undefined || section.current !== undefined) ChangesHelper._fail(name, "a section has either check or definition, not both");
			} else if (!ChangesHelper._definitions.has(section.definition)) {
				ChangesHelper._fail(name, "a section needs a definition made by define() or a check function");
			}
			list.push({ name, check: section.check === undefined ? null : section.check, definition: section.definition, saved: section.saved, current: section.current });
		}
		return Object.freeze({ compare: (options) => ChangesHelper._runSections(list, options) });
	}

	static _sideValue(value) {
		return typeof value === "function" ? value() : value;
	}

	// Sections in order, stops at the first change unless building or listing
	static _runSections(list, options) {
		const settings = this._readOptions(options);
		const passed = options === undefined ? {} : options;
		const result = { hasChanges: false, sections: {} };
		if (settings.build) result.changes = {};
		if (settings.paths) {
			result.paths = [];
			result.differences = [];
		}
		for (const section of list) {
			const sectionResult = section.check !== null ? section.check(passed) : section.definition.compare(this._sideValue(section.saved), this._sideValue(section.current), passed);
			if (sectionResult === null || typeof sectionResult !== "object" || typeof sectionResult.hasChanges !== "boolean") throw new Error(`${this.name}: section "${section.name}" did not return a result with hasChanges`);
			result.sections[section.name] = sectionResult.hasChanges;
			if (sectionResult.hasChanges) result.hasChanges = true;
			if (settings.build) result.changes[section.name] = sectionResult.changes;
			if (settings.paths && sectionResult.hasChanges) {
				if (Array.isArray(sectionResult.paths) && sectionResult.paths.length > 0) {
					for (const path of sectionResult.paths) result.paths.push(this._joinPath(section.name, path));
					for (const difference of sectionResult.differences || []) result.differences.push({ ...difference, path: this._joinPath(section.name, difference.path) });
				} else {
					result.paths.push(section.name);
					result.differences.push({ path: section.name, type: "changed", saved: undefined, current: undefined });
				}
			}
			if (result.hasChanges && !settings.build && !settings.paths) break;
		}
		return result;
	}

	// Snapshot

	static snapshot(value) {
		return ChangesHelper._copy(value, new Map());
	}

	// Deep copy of plain objects, arrays and dates
	static _copy(value, copies) {
		if (value === null || typeof value !== "object") return value;
		if (copies.has(value)) return copies.get(value);
		if (Array.isArray(value)) {
			const copy = [];
			copies.set(value, copy);
			for (const item of value) copy.push(this._copy(item, copies));
			return copy;
		}
		if (this._isDate(value)) return new Date(value.getTime());
		if (!this._isPlainObject(value)) return value;
		const copy = {};
		copies.set(value, copy);
		for (const key of Object.keys(value)) copy[key] = this._copy(value[key], copies);
		return copy;
	}

	static setAudit(on) {
		ChangesHelper._auditByDefault = on === true;
	}
}
