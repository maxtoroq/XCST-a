<?xml version="1.0" encoding="utf-8"?>
<!--
 Copyright 2015 Max Toro Q.

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
-->
<stylesheet version="2.0" exclude-result-prefixes="#all"
   xmlns="http://www.w3.org/1999/XSL/Transform"
   xmlns:xs="http://www.w3.org/2001/XMLSchema"
   xmlns:c="http://maxtoroq.github.io/XCST"
   xmlns:a="http://maxtoroq.github.io/XCST/application"
   xmlns:src="http://maxtoroq.github.io/XCST/compiled"
   xmlns:xcst="http://maxtoroq.github.io/XCST/syntax">

   <param name="a:application-uri" as="xs:anyURI"/>
   <param name="a:aspnetlib" select="true()" as="xs:boolean"/>

   <variable name="a:html-attributes" select="'class', 'attributes'"/>
   <variable name="a:input-attributes" select="'for', 'name', 'value', 'disabled', 'autofocus', $a:html-attributes"/>
   <variable name="a:text-box-attributes" select="'readonly', 'placeholder', $a:input-attributes"/>

   <!--
      ## Forms
   -->

   <template match="a:input | a:hidden" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <variable name="hidden" select="self::a:hidden"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="
            if ($hidden) then $a:input-attributes[. ne 'autofocus']
            else $a:text-box-attributes, 'format', 'type'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <value-of select="a:fully-qualified-helper('InputInstructions')"/>
         <text>.Input</text>
         <if test="@for">For</if>
         <if test="$for-model">ForModel</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <choose>
            <when test="@for">
               <text>, </text>
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <if test="@name">
                  <text>, </text>
                  <value-of select="src:expand-attribute(@name)"/>
               </if>
               <if test="@value">
                  <text>, </text>
                  <value-of select="xcst:expression(@value)"/>
               </if>
            </otherwise>
         </choose>
         <choose>
            <when test="$hidden">
               <text>, type: </text>
               <value-of select="src:string('hidden')"/>
               <call-template name="a:html-attributes-param">
                  <with-param name="bool-attributes" select="@disabled"/>
               </call-template>
            </when>
            <otherwise>
               <if test="@type">
                  <text>, type: </text>
                  <value-of select="@type/a:src.input-type(a:input-type(., true()), src:expand-attribute(.))"/>
               </if>
               <if test="@format">
                  <text>, format: </text>
                  <value-of select="src:expand-attribute(@format)"/>
               </if>
               <call-template name="a:html-attributes-param">
                  <with-param name="merge-attributes" select="@placeholder"/>
                  <with-param name="bool-attributes" select="@disabled, @readonly, @autofocus"/>
               </call-template>
            </otherwise>
         </choose>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:textarea" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:text-box-attributes, 'rows', 'cols'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper('TextAreaInstructions')"/>
         <text>.TextArea</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
               <text>, </text>
               <value-of select="(@value/xcst:expression(.), 'default(object)')[1]"/>
            </otherwise>
         </choose>
         <if test="@rows or @cols">
            <text>, rows: </text>
            <value-of select="(@rows/src:integer(xcst:integer(., true()), src:expand-attribute(.)), '2')[1]"/>
            <text>, columns: </text>
            <value-of select="(@cols/src:integer(xcst:integer(., true()), src:expand-attribute(.)), '20')[1]"/>
         </if>
         <call-template name="a:html-attributes-param">
            <with-param name="merge-attributes" select="@placeholder"/>
            <with-param name="bool-attributes" select="@disabled, @readonly, @autofocus"/>
         </call-template>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:check-box" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:input-attributes[. ne 'value'], 'checked'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @checked"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <value-of select="a:fully-qualified-helper('InputInstructions')"/>
         <text>.CheckBox</text>
         <if test="@for">For</if>
         <if test="$for-model">ForModel</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <choose>
            <when test="@for">
               <text>, </text>
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <if test="@name">
                  <text>, </text>
                  <value-of select="src:expand-attribute(@name)"/>
               </if>
               <if test="@checked">
                  <text>, isChecked: </text>
                  <value-of select="@checked/src:boolean(xcst:boolean(., true()), src:expand-attribute(.))"/>
               </if>
            </otherwise>
         </choose>
         <call-template name="a:html-attributes-param">
            <with-param name="bool-attributes" select="@disabled, @autofocus"/>
         </call-template>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:radio-button" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="required" select="'value'"/>
         <with-param name="optional" select="$a:input-attributes[. ne 'value'], 'checked'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @checked"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <value-of select="a:fully-qualified-helper('InputInstructions')"/>
         <text>.RadioButton</text>
         <if test="@for">For</if>
         <if test="$for-model">ForModel</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <choose>
            <when test="@for">
               <text>, </text>
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <when test="@name">
               <text>, </text>
               <value-of select="src:expand-attribute(@name)"/>
            </when>
         </choose>
         <text>, </text>
         <value-of select="xcst:expression(@value)"/>
         <if test="not(@for) and @checked">
            <text>, isChecked: </text>
            <value-of select="@checked/src:boolean(xcst:boolean(., true()), src:expand-attribute(.))"/>
         </if>
         <call-template name="a:html-attributes-param">
            <with-param name="bool-attributes" select="@disabled, @autofocus"/>
         </call-template>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:anti-forgery-token" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="extension" select="true()"/>
      </call-template>

      <choose>
         <when test="$a:aspnetlib">
            <variable name="output-is-doc" select="src:output-is-doc($output)"/>
            <variable name="doc-output" select="src:doc-output(., $output)"/>
            <if test="not($output-is-doc)">
               <c:variable name="{$doc-output}">
                  <attribute name="value">
                     <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
                     <text>.CastElement(</text>
                     <value-of select="$output"/>
                     <text>)</text>
                  </attribute>
               </c:variable>
            </if>
            <variable name="expr">
               <call-template name="a:html-helper"/>
               <text>.AntiForgeryToken(</text>
               <value-of select="$doc-output"/>
               <text>)</text>
            </variable>
            <c:void value="{$expr}"/>
         </when>
         <otherwise>
            <variable name="expr">
               <value-of select="src:global-identifier('System.Web.Helpers.AntiForgery')"/>
               <text>.GetHtml().ToString()</text>
            </variable>
            <c:value-of value="{$expr}" disable-output-escaping="yes"/>
         </otherwise>
      </choose>
   </template>

   <template match="a:select" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>
      <param name="indent" tunnel="yes"/>

      <variable name="for-model" select="empty((@for, @name))"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:input-attributes, 'options', 'multiple'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for">
         <with-param name="attribs" select="@name, @value"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper('SelectInstructions')"/>
         <text>.Select</text>
         <if test="@for or $for-model">For</if>
         <if test="$for-model">Model</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, xcst:expression(@for)" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
            </choose>
         </if>
         <variable name="multiple" select="(@multiple/xcst:boolean(.), false())[1]"/>
         <text>, </text>
         <call-template name="a:options">
            <with-param name="value" select="@value"/>
            <with-param name="multiple" select="$multiple"/>
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <if test="$multiple">
            <text>, multiple: </text>
            <value-of select="src:boolean($multiple)"/>
         </if>
         <call-template name="a:html-attributes-param">
            <with-param name="bool-attributes" select="@disabled, @autofocus"/>
         </call-template>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template name="a:options">
      <param name="value" as="attribute()?"/>
      <param name="multiple" select="false()"/>

      <choose>
         <when test="a:option or @options">
            <!--
               Casting of xcst:expression avoids turning into a dynamic object when one of the arguments is dynamic.
               A long method chain on a dynamic object hurts performance.
            -->
            <value-of select="a:fully-qualified-helper('OptionList')"/>
            <choose>
               <when test="a:option">
                  <text>.FromStaticList(</text>
                  <value-of select="src:integer(count(a:option))"/>
                  <text>)</text>
               </when>
               <otherwise>.Create()</otherwise>
            </choose>
            <if test="$value">
               <call-template name="src:line-number"/>
               <call-template name="src:new-line-indented"/>
               <text>.WithSelectedValue((object)</text>
               <text>(</text>
               <value-of select="xcst:expression($value)"/>
               <text>)</text>
               <if test="$multiple">
                  <text>, multiple: </text>
                  <value-of select="src:boolean($multiple)"/>
               </if>
               <text>)</text>
            </if>
            <for-each select="a:option">
               <call-template name="xcst:validate-attribs">
                  <with-param name="optional" select="'value', 'selected', 'disabled'"/>
                  <with-param name="extension" select="true()"/>
               </call-template>
               <call-template name="src:line-number"/>
               <call-template name="src:new-line-indented"/>
               <text>.AddStaticOption(</text>
               <if test="@value">
                  <text>value: (object)(</text>
                  <value-of select="xcst:expression(@value)"/>
                  <text>), </text>
               </if>
               <text>text: </text>
               <call-template name="src:simple-content"/>
               <if test="@selected">
                  <text>, selected: </text>
                  <value-of select="@selected/src:boolean(xcst:boolean(., true()), src:expand-attribute(.))"/>
               </if>
               <if test="@disabled">
                  <text>, disabled: </text>
                  <value-of select="@disabled/src:boolean(xcst:boolean(., true()), src:expand-attribute(.))"/>
               </if>
               <text>)</text>
            </for-each>
            <if test="@options">
               <call-template name="src:line-number"/>
               <call-template name="src:new-line-indented"/>
               <!-- Don't cast expression, behavior depends on overload resolution -->
               <text>.ConcatDynamicList(</text>
               <value-of select="xcst:expression(@options)"/>
               <text>)</text>
            </if>
         </when>
         <otherwise>
            <value-of select="concat('default(', src:global-identifier('System.Collections.Generic.IEnumerable'), '&lt;', src:global-identifier('System.Web.Mvc.SelectListItem'), '>)')"/>
         </otherwise>
      </choose>
   </template>

   <template match="a:label" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:html-attributes, 'for', 'name', 'text'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <value-of select="a:fully-qualified-helper('LabelInstructions')"/>
         <text>.Label</text>
         <choose>
            <when test="@for">For</when>
            <when test="$for-model">ForModel</when>
         </choose>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, @for" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
            </choose>
         </if>
         <if test="@text">
            <text>, labelText: </text>
            <value-of select="src:expand-attribute(@text)"/>
         </if>
         <call-template name="a:html-attributes-param"/>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:validation-summary" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:html-attributes, 'include-member-errors', 'message'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper('ValidationInstructions')"/>
         <text>.ValidationSummary(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <if test="@include-member-errors">
            <text>, includePropertyErrors: </text>
            <value-of select="@include-member-errors/src:boolean(xcst:boolean(., true()), src:expand-attribute(.))"/>
         </if>
         <text>, message: </text>
         <value-of select="(@message/src:expand-attribute(.), 'default(string)')[1]"/>
         <call-template name="a:html-attributes-param"/>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template match="a:validation-message" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="$a:html-attributes, 'for', 'name', 'message'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper('ValidationInstructions')"/>
         <text>.ValidationMessage</text>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <when test="@name">
               <value-of select="src:expand-attribute(@name)"/>
            </when>
            <otherwise>
               <value-of select="src:string('')"/>
            </otherwise>
         </choose>
         <text>, </text>
         <value-of select="(@message/src:expand-attribute(.), 'default(string)')[1]"/>
         <call-template name="a:html-attributes-param"/>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <!--
      ## Templates
   -->

   <template match="a:editor | a:display" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>

      <variable name="editor" select="self::a:editor"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'for', 'name', 'template', 'field-name', 'attributes', 'with-params', 'options', ('autofocus', 'disabled', 'readonly')[$editor]"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="output-is-doc" select="src:output-is-doc($output)"/>
      <variable name="doc-output" select="src:doc-output(., $output)"/>

      <if test="not($output-is-doc)">
         <c:variable name="{$doc-output}">
            <attribute name="value">
               <value-of select="src:fully-qualified-helper('DocumentWriter')"/>
               <text>.CastElement(</text>
               <value-of select="$output"/>
               <text>)</text>
            </attribute>
         </c:variable>
      </if>

      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <value-of select="a:fully-qualified-helper(concat((if ($editor) then 'Editor' else 'Display'), 'Instructions'))"/>
         <text>.</text>
         <value-of select="if ($editor) then 'Editor' else 'Display'"/>
         <if test="@for or $for-model">For</if>
         <if test="$for-model">Model</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <text>, </text>
         <value-of select="$doc-output"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, xcst:expression(@for)" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
            </choose>
         </if>
         <text>, templateName: </text>
         <choose>
            <when test="@template">
               <value-of select="src:expand-attribute(@template)"/>
            </when>
            <otherwise>null</otherwise>
         </choose>
         <if test="@field-name">
            <text>, htmlFieldName: </text>
            <value-of select="src:expand-attribute(@field-name)"/>
         </if>
         <call-template name="a:editor-additional-view-data"/>
         <text>)</text>
      </variable>
      <c:void value="{$expr}"/>
   </template>

   <template name="a:editor-additional-view-data">
      <variable name="editor" select="self::a:editor"/>
      <variable name="boolean-attribs" select="(@autofocus, @disabled, @readonly)[$editor]"/>
      <variable name="setters" as="text()*">
         <if test="$boolean-attribs">
            <value-of>
               <text>[</text>
               <value-of select="src:string('htmlAttributes')"/>
               <text>] = </text>
               <call-template name="a:html-attributes-param">
                  <with-param name="bool-attributes" select="$boolean-attribs"/>
                  <with-param name="class" select="()"/>
                  <with-param name="omit-param" select="true()"/>
               </call-template>
            </value-of>
         </if>
         <for-each select="@attributes[not($boolean-attribs)], a:with-options, .[@options], a:member-template">
            <variable name="setter">
               <apply-templates select="." mode="a:editor-additional-view-data"/>
            </variable>
            <if test="string($setter)">
               <value-of select="$setter"/>
            </if>
         </for-each>
      </variable>
      <if test="@with-params or $setters">
         <text>, additionalViewData: new </text>
         <value-of select="src:global-identifier('System.Web.Routing.RouteValueDictionary')"/>
         <if test="@with-params">
            <text>(</text>
            <value-of select="xcst:expression(@with-params)"/>
            <text>)</text>
         </if>
         <text> { </text>
         <value-of select="string-join($setters, ', ')"/>
         <text> }</text>
      </if>
   </template>

   <template match="@attributes" mode="a:editor-additional-view-data">
      <text>[</text>
      <value-of select="src:string('htmlAttributes')"/>
      <text>] = </text>
      <value-of select="xcst:expression(.)"/>
   </template>

   <template match="a:with-options | a:*[@options]" mode="a:editor-additional-view-data">
      <param name="indent" tunnel="yes"/>

      <if test="self::a:with-options">
         <call-template name="xcst:validate-attribs">
            <with-param name="optional" select="'for', 'name', 'options'"/>
            <with-param name="extension" select="true()"/>
         </call-template>
         <call-template name="a:validate-for">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
      </if>

      <variable name="for" select="@for or self::a:with-options/../@for"/>
      <variable name="name" select="@name or self::a:with-options/../@name"/>
      <variable name="for-model" select="not($for or $name)"/>

      <text>[</text>
      <value-of select="src:string(concat(src:aux-variable('options'), ':'))"/>
      <text> + </text>
      <value-of select="a:fully-qualified-helper('InputInstructions')"/>
      <text>.Name</text>
      <if test="$for or $for-model">For</if>
      <if test="$for-model">Model</if>
      <text>(</text>
      <call-template name="a:html-helper"/>
      <if test="not($for-model)">
         <text>, </text>
         <choose>
            <when test="$for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, self::a:with-options/../@for/xcst:expression(.), @for/xcst:expression(.)" separator="."/>
            </when>
            <when test="$name">
               <value-of select="self::a:with-options/../@name/src:expand-attribute(.), @name/src:expand-attribute(.)" separator="."/>
            </when>
         </choose>
      </if>
      <text>)] = </text>
      <call-template name="a:options">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
      </call-template>
   </template>

   <template match="a:member-template" mode="a:editor-additional-view-data">
      <param name="indent" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'helper-name'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <variable name="meta" as="element()">
         <xcst:delegate/>
      </variable>
      <variable name="new-output" select="src:template-output($meta, .)"/>
      <variable name="new-helper" select="(@helper-name/xcst:name(.), concat(src:aux-variable('model_helper'), '_', generate-id()))[1]"/>

      <text>[</text>
      <value-of select="src:string(src:aux-variable('member_template'))"/>
      <text>]</text>
      <text> = new </text>
      <value-of select="src:global-identifier(concat('System.Action&lt;', src:global-identifier('System.Web.Mvc.HtmlHelper'), ', ', $new-output/@type, '>'))"/>
      <text>((</text>
      <value-of select="$new-helper, $new-output" separator=", "/>
      <text>) => </text>
      <call-template name="src:sequence-constructor">
         <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         <with-param name="output" select="$new-output" tunnel="yes"/>
         <with-param name="a:html-helper" select="$new-helper" tunnel="yes"/>
      </call-template>
      <text>)</text>
   </template>

   <!--
      ## Metadata
   -->

   <template match="@display" mode="src:scaffold-column-attribute">
      <next-match/>
      <variable name="display" select="xcst:non-string(.)"/>
      <variable name="scaffold" select="if ($display = ('view-only', 'edit-only', 'hidden')) then true() else xcst:boolean(.)"/>
      <if test="$scaffold">
         <call-template name="src:line-hidden"/>
         <call-template name="src:new-line-indented"/>
         <text>[</text>
         <value-of select="src:global-identifier('Xcst.Web.Mvc.ShowFor')"/>
         <text>(Display = </text>
         <value-of select="src:boolean($display ne 'edit-only')"/>
         <text>, Edit = </text>
         <value-of select="src:boolean($display ne 'view-only')"/>
         <text>)]</text>
         <if test="$display eq 'hidden'">
            <call-template name="src:new-line-indented"/>
            <text>[</text>
            <value-of select="src:global-identifier('System.Web.Mvc.HiddenInput')"/>
            <text>(DisplayValue = false)]</text>
         </if>
      </if>
   </template>

   <template match="c:type | c:member" mode="src:type-attribute-extra">
      <next-match/>
      <variable name="excluded" select="c:member[@a:skip-binding[xcst:boolean(.)]]/xcst:name(@name)"/>
      <if test="not(empty($excluded))">
         <c:metadata name="{src:global-identifier('System.Web.Mvc.Bind')}"
            value="Exclude = {src:string(string-join($excluded, ','))}"/>
      </if>
   </template>

   <template match="c:member" mode="src:member-attribute-extra">
      <next-match/>
      <apply-templates select="@a:*" mode="a:src.member-attribute-extra"/>
   </template>

   <template match="@a:file-extensions" mode="a:src.member-attribute-extra">
      <variable name="setters" as="text()*">
         <value-of select="src:verbatim-string(xcst:non-string(.))"/>
         <call-template name="src:validation-setters">
            <with-param name="name" select="node-name(.)"/>
         </call-template>
      </variable>
      <c:metadata name="{src:global-identifier('Xcst.Web.Mvc.FileExtensionsAttribute')}"
         value="{string-join($setters, ', ')}"/>
   </template>

   <template match="@a:file-max-length" mode="a:src.member-attribute-extra">
      <variable name="setters" as="text()*">
         <value-of select="src:integer(xcst:integer(.))"/>
         <call-template name="src:validation-setters">
            <with-param name="name" select="node-name(.)"/>
         </call-template>
      </variable>
      <c:metadata name="{src:global-identifier('Xcst.Web.Mvc.FileMaxLengthAttribute')}"
         value="{string-join($setters, ', ')}"/>
   </template>

   <template match="@a:file-extensions-message | @a:file-max-length-message | @a:skip-binding" mode="a:src.member-attribute-extra"/>

   <template match="@a:*" mode="a:src.member-attribute-extra">
      <sequence select="error((), concat('Attribute ''a:', local-name(), ''' is not allowed on element ', name(..)), src:error-object(.))"/>
   </template>

   <template match="a:display-name" mode="src:extension-instruction">
      <param name="src:current-mode" as="xs:QName" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'for', 'name'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="statement" select="$src:current-mode eq xs:QName('src:statement')"/>

      <variable name="for-model" select="empty((@for, @name))"/>
      <variable name="expr">
         <value-of select="a:fully-qualified-helper('MetadataInstructions')"/>
         <text>.DisplayName</text>
         <if test="@for or $for-model">For</if>
         <if test="$for-model">Model</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <if test="not($for-model)">
            <text>, </text>
            <choose>
               <when test="@for">
                  <variable name="param" select="src:aux-variable(generate-id())"/>
                  <value-of select="$param"/>
                  <text> => </text>
                  <value-of select="$param, xcst:expression(@for)" separator="."/>
               </when>
               <when test="@name">
                  <value-of select="src:expand-attribute(@name)"/>
               </when>
            </choose>
         </if>
         <text>)</text>
      </variable>
      <choose>
         <when test="$statement">
            <c:object value="{$expr}"/>
         </when>
         <otherwise>
            <value-of select="$expr"/>
         </otherwise>
      </choose>
   </template>

   <template match="a:display-name" mode="xcst:extension-instruction">
      <xcst:instruction as="System.String" expression="true"/>
   </template>

   <template match="a:display-text" mode="src:extension-instruction">
      <param name="output" tunnel="yes"/>
      <param name="src:current-mode" as="xs:QName" required="yes" tunnel="yes"/>

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'for', 'name'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <call-template name="a:validate-for"/>

      <variable name="statement" select="$src:current-mode eq xs:QName('src:statement')"/>

      <variable name="expr">
         <value-of select="a:fully-qualified-helper('MetadataInstructions')"/>
         <text>.Display</text>
         <value-of select="if ($statement) then 'Text' else 'String'"/>
         <if test="@for">For</if>
         <text>(</text>
         <call-template name="a:html-helper"/>
         <if test="$statement">
            <text>, </text>
            <value-of select="$output"/>
         </if>
         <text>, </text>
         <choose>
            <when test="@for">
               <variable name="param" select="src:aux-variable(generate-id())"/>
               <value-of select="$param"/>
               <text> => </text>
               <value-of select="$param, xcst:expression(@for)" separator="."/>
            </when>
            <otherwise>
               <value-of select="(@name/src:expand-attribute(.), src:string(''))[1]"/>
            </otherwise>
         </choose>
         <text>)</text>
      </variable>
      <choose>
         <when test="$statement">
            <c:void value="{$expr}"/>
         </when>
         <otherwise>
            <value-of select="$expr"/>
         </otherwise>
      </choose>
   </template>

   <template match="a:display-text" mode="xcst:extension-instruction">
      <xcst:instruction as="System.String" expression="true"/>
   </template>

   <!--
      ## Models
   -->

   <template match="a:model" mode="src:extension-instruction">

      <call-template name="xcst:validate-attribs">
         <with-param name="optional" select="'value', 'as', 'field-prefix', 'helper-name', 'with-params'"/>
         <with-param name="extension" select="true()"/>
      </call-template>

      <if test="not(@value or @as)">
         <sequence select="error((), 'Element must have either @value or @as attributes.', src:error-object(.))"/>
      </if>

      <variable name="new-helper" select="(@helper-name/xcst:name(.), concat(src:aux-variable('model_helper'), '_', generate-id()))[1]"/>
      <variable name="type" select="@as/xcst:type(.)"/>
      <call-template name="src:new-line-indented"/>
      <text>var </text>
      <value-of select="$new-helper"/>
      <text> = </text>
      <value-of select="a:fully-qualified-helper('HtmlHelperFactory')"/>
      <text>.ForModel</text>
      <if test="$type">
         <value-of select="concat('&lt;', $type, '>')"/>
      </if>
      <text>(</text>
      <call-template name="a:html-helper"/>
      <text>, </text>
      <value-of select="(@value/xcst:expression(.), concat('default(', ($type, 'object')[1], ')'))[1]"/>
      <if test="@field-prefix">
         <text>, htmlFieldPrefix: </text>
         <value-of select="src:expand-attribute(@field-prefix)"/>
      </if>
      <if test="@with-params">
         <text>, additionalViewData: </text>
         <value-of select="xcst:expression(@with-params)"/>
      </if>
      <text>)</text>
      <value-of select="$src:statement-delimiter"/>
      <call-template name="src:sequence-constructor">
         <with-param name="ensure-block" select="true()"/>
         <with-param name="a:html-helper" select="$new-helper" tunnel="yes"/>
      </call-template>
   </template>

   <!--
      ## Infrastructure
   -->

   <template match="/" mode="src:main">
      <choose>
         <when test="not($src:named-package)">
            <next-match>
               <with-param name="a:directives" tunnel="yes">
                  <apply-templates select="processing-instruction()" mode="a:directive"/>
               </with-param>
            </next-match>
         </when>
         <otherwise>
            <next-match/>
         </otherwise>
      </choose>
   </template>

   <template match="processing-instruction(inherits) | processing-instruction(model)" mode="a:directive">
      <if test="preceding-sibling::processing-instruction()[name() eq name(current())]">
         <sequence select="error((), concat('Only one ''', name(), ''' directive is allowed.'), src:error-object(.))"/>
      </if>
      <if test="preceding-sibling::processing-instruction()[name() = ('inherits', 'model')]">
         <sequence select="error((), '''inherits'' and ''model'' directives are mutually exclusive.', src:error-object(.))"/>
      </if>
      <element name="{name()}" namespace="">
         <value-of select="xcst:non-string(.)"/>
      </element>
   </template>

   <template match="processing-instruction(sessionstate)" mode="a:directive">
      <if test="preceding-sibling::processing-instruction()[name() eq name(current())]">
         <sequence select="error((), concat('Only one ''', name(), ''' directive is allowed.'), src:error-object(.))"/>
      </if>
      <variable name="value" select="xcst:non-string(.)"/>
      <variable name="allowed" select="'Default', 'Required', 'ReadOnly', 'Disabled'"/>
      <if test="not($value = $allowed)">
         <sequence select="error((), concat('Invalid value for ''', name(), ''' directive. Must be one of (', string-join($allowed, '|'), ').'), src:error-object(.))"/>
      </if>
      <element name="{name()}" namespace="">
         <value-of select="$value"/>
      </element>
   </template>

   <template match="c:module | c:package" mode="src:import-namespace-extra">
      <param name="class" tunnel="yes"/>

      <next-match/>
      <if test="not($src:named-package)">
         <call-template name="src:new-line-indented"/>
         <text>using static </text>
         <value-of select="$class, a:functions-type-name(.)" separator="."/>
         <value-of select="$src:statement-delimiter"/>
      </if>
   </template>

   <template match="c:module | c:package" mode="src:base-types" as="xs:string*">
      <param name="a:directives" as="node()?" tunnel="yes"/>

      <if test="not($src:named-package)">
         <sequence select="
            if ($a:directives/inherits) then string($a:directives/inherits)
            else concat($src:base-types[1], '&lt;', ($a:directives/model, 'dynamic')[1], '>')"/>
         <sequence select="$src:base-types[position() gt 1]"/>
         <if test="$a:directives/sessionstate">
            <sequence select="src:global-identifier('Xcst.Web.ISessionStateAware')"/>
         </if>
      </if>
   </template>

   <template match="c:module | c:package" mode="src:infrastructure-extra">
      <param name="indent" tunnel="yes"/>
      <param name="modules" tunnel="yes"/>
      <param name="a:directives" as="node()?" tunnel="yes"/>

      <next-match/>
      <if test="not($src:named-package)">

         <variable name="module-pos" select="
            for $pos in (1 to count($modules)) 
            return if ($modules[$pos] is current()) then $pos else ()"/>
         <variable name="principal-module" select="$module-pos eq count($modules)"/>

         <if test="$a:aspnetlib
            and $a:directives/sessionstate
            and $principal-module">

            <value-of select="$src:new-line"/>
            <call-template name="src:new-line-indented"/>
            <value-of select="src:global-identifier('System.Web.SessionState.SessionStateBehavior')"/>
            <text> </text>
            <value-of select="src:global-identifier('Xcst.Web.ISessionStateAware'), 'SessionStateBehavior'" separator="."/>
            <call-template name="src:open-brace"/>
            <call-template name="src:new-line-indented">
               <with-param name="increase" select="1"/>
            </call-template>
            <text>get { return </text>
            <value-of select="src:global-identifier('System.Web.SessionState.SessionStateBehavior'), $a:directives/sessionstate" separator="."/>
            <value-of select="$src:statement-delimiter"/>
            <text> }</text>
            <call-template name="src:close-brace"/>
         </if>

         <variable name="module-uri" select="document-uri(root(.))"/>
         <variable name="functions-type" select="a:functions-type-name(.)"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented"/>
         <text>internal static class </text>
         <value-of select="$functions-type"/>
         <call-template name="src:open-brace"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>static readonly string BasePath = </text>
         <value-of select="src:global-identifier('System.Web.VirtualPathUtility')"/>
         <text>.ToAbsolute(</text>
         <value-of select="src:verbatim-string(concat('~/', src:make-relative-uri($a:application-uri, $module-uri)))"/>
         <text>)</text>
         <value-of select="$src:statement-delimiter"/>
         <value-of select="$src:new-line"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="1"/>
         </call-template>
         <text>public static string Href(string path, params object[] pathParts)</text>
         <call-template name="src:open-brace"/>
         <call-template name="src:new-line-indented">
            <with-param name="increase" select="2"/>
         </call-template>
         <text>return </text>
         <value-of select="a:fully-qualified-helper('UrlUtil')"/>
         <text>.GenerateClientUrl(</text>
         <value-of select="$functions-type, 'BasePath'" separator="."/>
         <text>, path, pathParts)</text>
         <value-of select="$src:statement-delimiter"/>
         <call-template name="src:close-brace">
            <with-param name="indent" select="$indent + 1" tunnel="yes"/>
         </call-template>
         <call-template name="src:close-brace"/>
      </if>
   </template>

   <function name="a:functions-type-name">
      <param name="module" as="element()"/>

      <sequence select="concat('__xcst_functions_', generate-id($module))"/>
   </function>

   <!--
      ## Helpers
   -->

   <function name="a:input-type" as="xs:string?">
      <param name="node" as="node()"/>
      <param name="avt" as="xs:boolean"/>

      <variable name="string" select="xcst:non-string($node)"/>
      <sequence select="
         if ($avt and xcst:is-value-template($node)) then ()
         else $string
      "/>
   </function>

   <function name="a:src.input-type" as="xs:string">
      <param name="type" as="xs:string?"/>
      <param name="string" as="xs:string"/>

      <choose>
         <when test="$type instance of xs:string">
            <sequence select="src:verbatim-string($type)"/>
         </when>
         <otherwise>
            <sequence select="$string"/>
         </otherwise>
      </choose>
   </function>

   <template name="a:validate-for">
      <param name="attribs" select="@name" as="attribute()*"/>

      <if test="@for and $attribs">
         <sequence select="error((), concat('@for and @', $attribs[1]/local-name(), ' attributes are mutually exclusive.'), src:error-object(.))"/>
      </if>
   </template>

   <template name="a:html-helper">
      <param name="a:html-helper" as="xs:string?" tunnel="yes"/>

      <choose>
         <when test="$a:html-helper">
            <value-of select="$a:html-helper"/>
         </when>
         <otherwise>
            <text>((</text>
            <value-of select="src:global-identifier('Xcst.Web.Mvc.XcstViewPage')"/>
            <text>)</text>
            <value-of select="$src:context-field"/>
            <text>.TopLevelPackage).Html</text>
         </otherwise>
      </choose>
   </template>

   <template name="a:html-attributes-param">
      <param name="attributes" select="@attributes" as="attribute()?"/>
      <param name="class" select="@class" as="attribute()?"/>
      <param name="merge-attributes" as="attribute()*"/>
      <param name="bool-attributes" as="attribute()*"/>
      <param name="omit-param" select="false()"/>

      <if test="not(empty(($attributes, $class, $merge-attributes, $bool-attributes)))">
         <if test="not($omit-param)">
            <text>, htmlAttributes: </text>
         </if>
         <value-of select="a:fully-qualified-helper('HtmlAttributesMerger')"/>
         <text>.Create(</text>
         <value-of select="$attributes/xcst:expression(.)"/>
         <text>)</text>
         <if test="$class">
            <text>.AddCssClass(</text>
            <value-of select="src:expand-attribute($class)"/>
            <text>)</text>
         </if>
         <for-each select="$merge-attributes">
            <text>.MergeAttribute(</text>
            <value-of select="src:string(local-name())"/>
            <text>, </text>
            <value-of select="src:expand-attribute(.)"/>
            <text>)</text>
         </for-each>
         <for-each select="$bool-attributes">
            <text>.MergeBoolean(</text>
            <value-of select="src:string(local-name())"/>
            <text>, </text>
            <value-of select="src:boolean(xcst:boolean(., true()), src:expand-attribute(.))"/>
            <text>)</text>
         </for-each>
         <text>.Attributes</text>
      </if>
   </template>

   <function name="a:fully-qualified-helper" as="xs:string">
      <param name="helper" as="xs:string"/>

      <sequence select="concat(src:global-identifier('Xcst.Web.Runtime'), '.', $helper)"/>
   </function>

</stylesheet>