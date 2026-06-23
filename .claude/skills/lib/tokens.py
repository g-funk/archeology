#!/usr/bin/env python3
"""Shared tokenization library for config binary encoders.

Token ID ranges (CONFIG_STRINGS.md):
  0   –  999   predefined no-space  — punctuation, attaches to preceding token without a space
  1000 – 1999  predefined normal    — common words, preceded by a space when rendered
  2000 – 19999 user tokens          — stored in the binary token table
  >= 20000     token list pointer   — index into the token list table (offset by 20000)

A ushort string field stores either a single token ID (< 20000) or a token list
pointer (>= 20000, value minus 20000 is the index into the token list table).
"""
import json
import os
import struct
import time


def _default_tokens_path():
    lib_dir = os.path.dirname(os.path.abspath(__file__))
    return os.path.normpath(os.path.join(lib_dir, '..', '..', '..', 'config', 'json', 'predefined_tokens.json'))


def load_predefined(json_path=None):
    """Load predefined tokens from JSON. Returns (no_space, normal) as value→id dicts."""
    if json_path is None:
        json_path = _default_tokens_path()
    with open(json_path, encoding='utf-8') as f:
        data = json.load(f)
    no_space = {entry['value']: entry['id'] for entry in data['no_space']}
    normal   = {entry['value']: entry['id'] for entry in data['normal']}
    return no_space, normal


class Tokenizer:
    USER_TOKEN_START    = 2000
    TOKEN_LIST_PTR_BASE = 20000

    def __init__(self, no_space, normal, tokenized=True):
        self._tokenized      = tokenized
        self._no_space       = no_space   # value → id (0–999)
        self._normal         = normal     # value → id (1000–1999)
        self._no_space_chars = {v for v in no_space if len(v) == 1}
        self._user_tokens    = {}         # word → id (2000+)
        self._next_user_id   = self.USER_TOKEN_START
        self._token_lists    = []         # list of list[int]
        self._token_list_idx = {}         # tuple(ids) → index in _token_lists

    # ------------------------------------------------------------------
    # Public API
    # ------------------------------------------------------------------

    def tokenize(self, text):
        """Tokenize text and return a ushort string pointer.

        In naive mode (tokenized=False) the whole string is stored as a single
        user token with no splitting or predefined-token substitution.
        """
        if not self._tokenized:
            return self._intern(text)
        ids = []
        for word in text.split():
            ids.extend(self._split_word(word))
        return self._to_ptr(ids)

    def encode_tables(self):
        """Return binary encoding of user token table + token list table."""
        buf = bytearray()

        sorted_tokens = sorted(self._user_tokens.items(), key=lambda kv: kv[1])
        buf += struct.pack('<H', len(sorted_tokens))
        for word, _ in sorted_tokens:
            encoded = word.encode('utf-8')
            buf += struct.pack('<B', len(encoded))
            buf += encoded

        buf += struct.pack('<H', len(self._token_lists))
        for token_list in self._token_lists:
            buf += struct.pack('<B', len(token_list))
            for tid in token_list:
                buf += struct.pack('<H', tid)

        return bytes(buf)

    @property
    def user_token_count(self):
        return len(self._user_tokens)

    @property
    def token_list_count(self):
        return len(self._token_lists)

    # ------------------------------------------------------------------
    # Internal helpers
    # ------------------------------------------------------------------

    def _split_word(self, word):
        """Peel trailing single-char no-space punctuation; look up remainder."""
        i = len(word)
        trailing = []
        while i > 1 and word[i - 1] in self._no_space_chars:
            i -= 1
            trailing.insert(0, word[i])

        result = []
        word_part = word[:i]
        if word_part:
            result.append(self._lookup(word_part))
        for p in trailing:
            result.append(self._no_space[p])
        return result

    def _intern(self, text):
        """Store the whole string as a single user token (naive mode)."""
        if text not in self._user_tokens:
            self._user_tokens[text] = self._next_user_id
            self._next_user_id += 1
        return self._user_tokens[text]

    def _lookup(self, word):
        """Return token ID for a word; assign a user token ID if unknown."""
        if word in self._no_space:
            return self._no_space[word]
        if word in self._normal:
            return self._normal[word]
        if word not in self._user_tokens:
            self._user_tokens[word] = self._next_user_id
            self._next_user_id += 1
        return self._user_tokens[word]

    def _to_ptr(self, ids):
        if len(ids) == 1:
            return ids[0]
        key = tuple(ids)
        if key not in self._token_list_idx:
            self._token_list_idx[key] = len(self._token_lists)
            self._token_lists.append(ids)
        return self.TOKEN_LIST_PTR_BASE + self._token_list_idx[key]


def encode_header(version_major, version_minor):
    """Encode the config file prefix: version bytes + build epoch (little-endian)."""
    buf = bytearray()
    buf += struct.pack('<B', version_major)
    buf += struct.pack('<B', version_minor)
    buf += struct.pack('<q', int(time.time()))
    return bytes(buf)
