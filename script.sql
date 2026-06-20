-- DROP DATABASE IF EXISTS bdFiestaMexicana;
CREATE DATABASE bdFiestaMexicana;
USE bdFiestaMexicana;

CREATE TABLE categoria (
    id        INT PRIMARY KEY AUTO_INCREMENT,
    nome      VARCHAR(150) NOT NULL,
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE metodo_preparo (
    id        INT PRIMARY KEY AUTO_INCREMENT,
    nome      VARCHAR(100) NOT NULL,   -- Ex: Grelhado, Assado, Frito, Cru
    criado_em DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Usuarios (
    id         INT PRIMARY KEY AUTO_INCREMENT,
    nome       VARCHAR(100),
    email      VARCHAR(100) UNIQUE,
    senha_hash VARCHAR(255),
    role       ENUM('Garcom', 'Chefe', 'Admin'),
    ativo      TINYINT(1) DEFAULT 1,
    criado_em  DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Mesa (
    id         INT PRIMARY KEY AUTO_INCREMENT,
    numero     INT NOT NULL,
    capacidade INT NOT NULL,
    status     ENUM('Livre', 'Ocupado') NOT NULL DEFAULT 'Livre',
    criado_em  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);


CREATE TABLE Garcom (
	id        INT PRIMARY KEY AUTO_INCREMENT,
    nome      VARCHAR(100) NOT NULL,
    cpf       CHAR(11) UNIQUE,
    turno     ENUM('Manhã', 'Tarde', 'Noite') NOT NULL,
    criado_em DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Prato (
    id              INT PRIMARY KEY AUTO_INCREMENT,
    nome            VARCHAR(200) NOT NULL,
    preco           DECIMAL(10,2) NOT NULL,
    descricao       TEXT,
    categoria       INT,
    metodo_preparo  INT,
    nivel_picancia  ENUM('Sem Pimenta','Suave','Médio','Forte','Extra') DEFAULT 'Sem Pimenta',
    tempo_preparo   INT COMMENT 'Tempo em minutos',
    disponivel BOOL NOT NULL,
    capa_arquivo    VARCHAR(255) NULL,
    criado_em       DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (categoria)      REFERENCES categoria(id),
    FOREIGN KEY (metodo_preparo) REFERENCES metodo_preparo(id)
);


CREATE TABLE Pedido (
    id         INT PRIMARY KEY AUTO_INCREMENT,
    mesa       INT,
    garcom     INT,
    status     ENUM('Pendente','Preparando','Finalizado','Cancelado') DEFAULT 'Pendente',
    observacao VARCHAR(255) NULL COMMENT 'Ex: sem coentro, molho à parte',
    total      DECIMAL(10,2),
    data_hora  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    criado_em  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (mesa)   REFERENCES Mesa(id),
    FOREIGN KEY (garcom) REFERENCES Garcom(id)
);

CREATE TABLE Pedido_itens (
    id             INT PRIMARY KEY AUTO_INCREMENT,
    pedido         INT NOT NULL,
    prato          INT NOT NULL,
    quantidade     INT NOT NULL,
    preco_unitario DECIMAL(10,2) NOT NULL,
    subtotal       DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (pedido) REFERENCES Pedido(id),
    FOREIGN KEY (prato)  REFERENCES Prato(id)
);

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_usuario_criar $$
CREATE PROCEDURE sp_usuario_criar (
    IN p_nome VARCHAR(100),
    IN p_email VARCHAR(100),
    IN p_senha_hash VARCHAR(255),
    IN p_role VARCHAR(20)
)
BEGIN
    INSERT INTO Usuarios (nome, email, senha_hash, role, ativo, criado_em)
    VALUES (p_nome, p_email, p_senha_hash, p_role, 1, NOW());
END $$

DROP PROCEDURE IF EXISTS sp_usuario_obter_por_email $$
CREATE PROCEDURE sp_usuario_obter_por_email(IN p_email VARCHAR(100))
BEGIN
  SELECT id, nome, email, senha_hash, role, ativo
  FROM usuarios
  WHERE email = p_email
  LIMIT 1;
END $$

DELIMITER ;

-- exemplo de uso (ATENÇÃO: role deve ser 'Adm', não 'Admin')
CALL sp_usuario_criar(
 'Juan Pablo Admin',
 'juanpablo@fiesta.com',
 '$2a$11$Q91fiPYPec73pUA4DKByXeSNOZ6TYn2ZY5jWSWpr57rkfUEyKjWq2',
 'Admin'
);

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_prato_listar $$
CREATE PROCEDURE sp_prato_listar()
BEGIN
    SELECT
        p.id,
        p.nome,
        p.preco,
        p.descricao,
        p.categoria,
        c.nome AS categoria_nome, 
        p.metodo_preparo,
        mp.nome AS metodo_preparo_nome, 
        p.nivel_picancia,
        p.tempo_preparo,
        p.disponivel,
        p.capa_arquivo,
        p.criado_em
    FROM Prato p
    LEFT JOIN categoria c ON c.id = p.categoria
    LEFT JOIN metodo_preparo mp ON mp.id = p.metodo_preparo
    ORDER BY p.nome;
END $$


DROP PROCEDURE IF EXISTS sp_categoria_listar $$
CREATE PROCEDURE sp_categoria_listar()
BEGIN
    SELECT id, nome FROM categoria ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_metodo_preparo_listar $$
CREATE PROCEDURE sp_metodo_preparo_listar()
BEGIN
    SELECT id, nome FROM metodo_preparo ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_prato_listar $$
CREATE PROCEDURE sp_prato_listar()
BEGIN
    SELECT
        p.id,
        p.nome,
        p.preco,
        p.descricao,
        p.categoria,
        c.nome AS categoria_nome,
        p.metodo_preparo,
        mp.nome AS metodo_preparo_nome,
        p.nivel_picancia,
        p.tempo_preparo,
        p.disponivel,
        p.capa_arquivo,
        p.criado_em
    FROM Prato p
    LEFT JOIN categoria c ON c.id = p.categoria
    LEFT JOIN metodo_preparo mp ON mp.id = p.metodo_preparo
    ORDER BY p.nome;
END $$

-- Procedure para Criar um Prato
DROP PROCEDURE IF EXISTS sp_prato_criar $$
CREATE PROCEDURE sp_prato_criar (
    IN p_nome            VARCHAR(200),
    IN p_preco           DECIMAL(10,2),
    IN p_descricao       TEXT,
    IN p_categoria       INT,
    IN p_metodo_preparo  INT,
    IN p_nivel_picancia  ENUM('Sem Pimenta','Suave','Médio','Forte','Extra'),
    IN p_tempo_preparo   INT,
    IN p_disponivel      BOOL,
    IN p_capa_arquivo    VARCHAR(255)
)
BEGIN
    INSERT INTO Prato (
        nome, preco, descricao, categoria, metodo_preparo,
        nivel_picancia, tempo_preparo, disponivel, capa_arquivo
    )
    VALUES (
        p_nome, p_preco, p_descricao, p_categoria, p_metodo_preparo,
        p_nivel_picancia, p_tempo_preparo, p_disponivel, p_capa_arquivo
    );
END $$

DROP PROCEDURE IF EXISTS sp_prato_obter $$
CREATE PROCEDURE sp_prato_obter (
    IN p_id INT
)
BEGIN
    SELECT
        p.id,
        p.nome,
        p.preco,
        p.descricao,
        p.categoria,
        c.nome AS categoria_nome,
        p.metodo_preparo,
        mp.nome AS metodo_preparo_nome,
        p.nivel_picancia,
        p.tempo_preparo,
        p.disponivel,
        p.capa_arquivo,
        p.criado_em
    FROM Prato p
    LEFT JOIN categoria c ON c.id = p.categoria
    LEFT JOIN metodo_preparo mp ON mp.id = p.metodo_preparo
    WHERE p.id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_prato_atualizar $$
CREATE PROCEDURE sp_prato_atualizar (
    IN p_id              INT,
    IN p_nome            VARCHAR(200),
    IN p_preco           DECIMAL(10,2),
    IN p_descricao       TEXT,
    IN p_categoria       INT,
    IN p_metodo_preparo  INT,
    IN p_nivel_picancia  ENUM('Sem Pimenta','Suave','Médio','Forte','Extra'),
    IN p_tempo_preparo   INT,
    IN p_disponivel      BOOL,
    IN p_capa_arquivo    VARCHAR(255)
)
BEGIN
    UPDATE Prato
    SET
        nome            = p_nome,
        preco           = p_preco,
        descricao       = p_descricao,
        categoria       = p_categoria,
        metodo_preparo  = p_metodo_preparo,
        nivel_picancia  = p_nivel_picancia,
        tempo_preparo   = p_tempo_preparo,
        disponivel      = p_disponivel,
        capa_arquivo    = p_capa_arquivo
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_prato_excluir $$
CREATE PROCEDURE sp_prato_excluir (
    IN p_id INT
)
BEGIN
    DELETE FROM Prato
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_categoria_listar $$
CREATE PROCEDURE sp_categoria_listar()
BEGIN
    SELECT id, nome, criado_em
    FROM categoria
    ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_categoria_criar $$
CREATE PROCEDURE sp_categoria_criar (
    IN p_nome VARCHAR(150)
)
BEGIN
    INSERT INTO categoria (nome)
    VALUES (p_nome);
END $$

DROP PROCEDURE IF EXISTS sp_categoria_obter $$
CREATE PROCEDURE sp_categoria_obter (
    IN p_id INT
)
BEGIN
    SELECT id, nome, criado_em
    FROM categoria
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_categoria_atualizar $$
CREATE PROCEDURE sp_categoria_atualizar (
    IN p_id   INT,
    IN p_nome VARCHAR(150)
)
BEGIN
    UPDATE categoria
    SET nome = p_nome
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_categoria_excluir $$
CREATE PROCEDURE sp_categoria_excluir (
    IN p_id INT
)
BEGIN
    DELETE FROM categoria
    WHERE id = p_id;
END $$


DROP PROCEDURE IF EXISTS sp_metodo_preparo_listar $$
CREATE PROCEDURE sp_metodo_preparo_listar()
BEGIN
    SELECT id, nome, criado_em
    FROM metodo_preparo
    ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_metodo_preparo_criar $$
CREATE PROCEDURE sp_metodo_preparo_criar (
    IN p_nome VARCHAR(100)
)
BEGIN
    INSERT INTO metodo_preparo (nome)
    VALUES (p_nome);
END $$

DROP PROCEDURE IF EXISTS sp_metodo_preparo_obter $$
CREATE PROCEDURE sp_metodo_preparo_obter (
    IN p_id INT
)
BEGIN
    SELECT id, nome, criado_em
    FROM metodo_preparo
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_metodo_preparo_atualizar $$
CREATE PROCEDURE sp_metodo_preparo_atualizar (
    IN p_id   INT,
    IN p_nome VARCHAR(100)
)
BEGIN
    UPDATE metodo_preparo
    SET nome = p_nome
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_metodo_preparo_excluir $$
CREATE PROCEDURE sp_metodo_preparo_excluir (
    IN p_id INT
)
BEGIN
    DELETE FROM metodo_preparo
    WHERE id = p_id;
END $$

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_mesa_listar $$
CREATE PROCEDURE sp_mesa_listar()
BEGIN
    SELECT id, numero, capacidade, status, criado_em
    FROM Mesa
    ORDER BY numero;
END $$

DROP PROCEDURE IF EXISTS sp_mesa_criar $$
CREATE PROCEDURE sp_mesa_criar (
    IN p_numero     INT,
    IN p_capacidade INT,
    IN p_status     ENUM('Livre','Ocupado')
)
BEGIN
    INSERT INTO Mesa (numero, capacidade, status)
    VALUES (p_numero, p_capacidade, p_status);
END $$

DROP PROCEDURE IF EXISTS sp_mesa_obter $$
CREATE PROCEDURE sp_mesa_obter (IN p_id INT)
BEGIN
    SELECT id, numero, capacidade, status, criado_em
    FROM Mesa
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_mesa_atualizar $$
CREATE PROCEDURE sp_mesa_atualizar (
    IN p_id         INT,
    IN p_numero     INT,
    IN p_capacidade INT,
    IN p_status     ENUM('Livre','Ocupado')
)
BEGIN
    UPDATE Mesa
    SET numero     = p_numero,
        capacidade = p_capacidade,
        status     = p_status
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_mesa_excluir $$
CREATE PROCEDURE sp_mesa_excluir (IN p_id INT)
BEGIN
    DELETE FROM Mesa
    WHERE id = p_id;
END $$

delimiter ;

delimiter $$

DROP PROCEDURE IF EXISTS sp_garcom_listar $$
CREATE PROCEDURE sp_garcom_listar()
BEGIN
    SELECT id, nome, cpf, turno, criado_em
    FROM Garcom
    ORDER BY nome;
END $$

delimiter ;

delimiter $$

DROP PROCEDURE IF EXISTS sp_garcom_criar $$
CREATE PROCEDURE sp_garcom_criar (
    IN p_nome  VARCHAR(100),
    IN p_cpf   CHAR(11),
    IN p_turno VARCHAR(20)
)
BEGIN
    INSERT INTO Garcom (nome, cpf, turno)
    VALUES (p_nome, p_cpf, p_turno);
END $$

DROP PROCEDURE IF EXISTS sp_garcom_obter $$
CREATE PROCEDURE sp_garcom_obter (
    IN p_id INT
)
BEGIN
    SELECT id, nome, cpf, turno, criado_em
    FROM Garcom
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_garcom_atualizar $$
CREATE PROCEDURE sp_garcom_atualizar (
    IN p_id    INT,
    IN p_nome  VARCHAR(100),
    IN p_cpf   CHAR(11),
    IN p_turno VARCHAR(20)
)
BEGIN
    UPDATE Garcom
    SET
        nome  = p_nome,
        cpf   = p_cpf,
        turno = p_turno
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_garcom_excluir $$
CREATE PROCEDURE sp_garcom_excluir (
    IN p_id INT
)
BEGIN
    DELETE FROM Garcom
    WHERE id = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_pedido_criar $$
CREATE PROCEDURE sp_pedido_criar(
    IN  p_mesa INT,
    IN  p_garcom INT,
    IN  p_observacao VARCHAR(255),
    OUT p_id_gerado INT
)
BEGIN
    INSERT INTO Pedido (mesa, garcom, status, observacao, total)
    VALUES (p_mesa, p_garcom, 'Pendente', p_observacao, 0.00);
    
    SET p_id_gerado = LAST_INSERT_ID();
END $$

DROP PROCEDURE IF EXISTS sp_pedido_adicionar_item $$
CREATE PROCEDURE sp_pedido_adicionar_item(
    IN p_id_pedido INT,
    IN p_id_prato INT,
    IN p_quantidade INT,
    IN p_preco_unitario DECIMAL(10,2),
    IN p_subtotal DECIMAL(10,2)
)
BEGIN
    DECLARE v_disp BOOL;

    IF p_quantidade IS NULL OR p_quantidade <= 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Quantidade inválida.';
    END IF;

    -- Verifica disponibilidade do prato (FOR UPDATE para evitar concorrência)
    SELECT disponivel INTO v_disp FROM Prato WHERE id = p_id_prato FOR UPDATE;
    
    IF v_disp IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Prato não encontrado.';
    END IF;
    
    IF v_disp = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Este prato não está disponível no momento.';
    END IF;

    INSERT INTO Pedido_itens (pedido, prato, quantidade, preco_unitario, subtotal)
    VALUES (p_id_pedido, p_id_prato, p_quantidade, p_preco_unitario, p_subtotal);

    UPDATE Pedido 
    SET total = (SELECT COALESCE(SUM(subtotal), 0) FROM Pedido_itens WHERE pedido = p_id_pedido)
    WHERE id = p_id_pedido;
END $$

DROP PROCEDURE IF EXISTS sp_prato_listar_cardapio $$
CREATE PROCEDURE sp_prato_listar_cardapio(IN p_q VARCHAR(200))
BEGIN
    SELECT id, nome, preco, capa_arquivo 
    FROM Prato
    WHERE disponivel = 1
      AND (p_q IS NULL OR p_q = '' OR nome LIKE CONCAT('%', p_q, '%'))
    ORDER BY nome;
END $$

DROP PROCEDURE IF EXISTS sp_prato_listar_por_ids $$
CREATE PROCEDURE sp_prato_listar_por_ids(IN p_ids TEXT)
BEGIN
    SELECT id, nome, preco, capa_arquivo 
    FROM Prato
    WHERE FIND_IN_SET(id, p_ids) > 0
    ORDER BY nome;
END $$

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_cozinha_listar_pedidos $$
CREATE PROCEDURE sp_cozinha_listar_pedidos()
BEGIN
    -- Agora listamos os pedidos que possuem pelo menos um item não finalizado
    SELECT DISTINCT
        p.id,
        m.numero   AS mesa_numero,
        g.nome     AS garcom_nome,
        p.status,
        p.observacao,
        p.total,
        p.data_hora
    FROM Pedido p
    INNER JOIN Mesa   m ON m.id = p.mesa
    INNER JOIN Garcom g ON g.id = p.garcom
    INNER JOIN Pedido_itens pi ON pi.pedido = p.id
    WHERE pi.status IN ('Pendente', 'Preparando')
       OR DATE(p.data_hora) = CURDATE()
    ORDER BY p.data_hora DESC;
END $$
delimiter ;

delimiter $$

DROP PROCEDURE IF EXISTS sp_cozinha_listar_itens $$
CREATE PROCEDURE sp_cozinha_listar_itens(IN p_id_pedido INT)
BEGIN
    SELECT
        pi.id         AS item_id, 
        pi.prato      AS prato_id,
        pr.nome       AS prato_nome,
        pi.quantidade,
        pi.status     AS item_status
    FROM Pedido_itens pi
    INNER JOIN Prato pr ON pr.id = pi.prato
    WHERE pi.pedido = p_id_pedido 
      AND pi.status IN ('Pendente', 'Preparando');
END $$

delimiter ;

delimiter $$

DROP PROCEDURE IF EXISTS sp_cozinha_atualizar_status $$
CREATE PROCEDURE sp_cozinha_atualizar_status(
    IN p_id     INT,
    IN p_status ENUM('Pendente','Preparando','Finalizado','Cancelado')
)
BEGIN
    UPDATE Pedido
    SET status = p_status
    WHERE id = p_id;
END $$

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_prato_listar_cardapio_categorias $$
CREATE PROCEDURE sp_prato_listar_cardapio_categorias()
BEGIN
    SELECT 
        p.id,
        p.nome,
        p.preco,
        p.capa_arquivo,
        p.descricao,
        p.tempo_preparo,
        p.nivel_picancia,
        COALESCE(c.nome, 'Outros') AS categoria_nome
    FROM Prato p
    LEFT JOIN categoria c ON c.id = p.categoria
    WHERE p.disponivel = 1
    ORDER BY categoria_nome, p.nome;
END $$

DROP PROCEDURE IF EXISTS sp_pratos_mais_pedidos $$
CREATE PROCEDURE sp_pratos_mais_pedidos()
BEGIN
    SELECT
        p.id,
        p.nome,
        p.preco,
        p.capa_arquivo,
        COALESCE(c.nome, 'Outros') AS categoria_nome,
        SUM(pi.quantidade) AS total_pedidos
    FROM Pedido_itens pi
    INNER JOIN Prato p  ON p.id  = pi.prato
    LEFT  JOIN categoria c ON c.id = p.categoria
    WHERE p.disponivel = 1
    GROUP BY p.id, p.nome, p.preco, p.capa_arquivo, c.nome
    ORDER BY total_pedidos DESC
    LIMIT 15;
END $$

DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_prato_obter_por_id $$
CREATE PROCEDURE sp_prato_obter_por_id(IN p_id INT)
BEGIN
    SELECT
        p.id,
        p.nome,
        p.preco,
        p.capa_arquivo,
        p.descricao,
        p.nivel_picancia,
        p.tempo_preparo,
        COALESCE(c.nome, '') AS categoria_nome
    FROM Prato p
    LEFT JOIN categoria c ON c.id = p.categoria
    WHERE p.id = p_id;
END $$

DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_dashboard_mesas $$
CREATE PROCEDURE sp_dashboard_mesas()
BEGIN
    SELECT
        SUM(status = 'Livre')   AS livres,
        SUM(status = 'Ocupado') AS ocupadas,
        COUNT(*)                AS total
    FROM Mesa;
END $$

DROP PROCEDURE IF EXISTS sp_dashboard_top_pratos $$
CREATE PROCEDURE sp_dashboard_top_pratos()
BEGIN
    SELECT
        pr.nome,
        SUM(pi.quantidade) AS total_vendido,
        SUM(pi.subtotal)   AS receita
    FROM Pedido_itens pi
    INNER JOIN Prato pr  ON pr.id  = pi.prato
    INNER JOIN Pedido p  ON p.id   = pi.pedido
    WHERE DATE(p.data_hora) = CURDATE()
      AND p.status != 'Cancelado'
    GROUP BY pr.id, pr.nome
    ORDER BY total_vendido DESC
    LIMIT 5;
END $$

DELIMITER ;

DROP PROCEDURE IF EXISTS sp_dashboard_por_garcom;
DELIMITER $$
CREATE PROCEDURE sp_dashboard_por_garcom()
BEGIN
    SELECT
        g.nome                              AS nome,
        COUNT(p.id)                         AS total_pedidos,
        SUM(p.total + p.taxa_servico)       AS total_valor
    FROM Garcom g
    JOIN Pedido p ON g.id = p.garcom
    WHERE DATE(p.data_hora) = CURDATE()
      AND p.status = 'Finalizado'
      AND p.eh_adicional = 0
    GROUP BY g.id, g.nome
    ORDER BY total_valor DESC;
END $$
DELIMITER ;

DROP PROCEDURE IF EXISTS sp_dashboard_ultimos_pedidos;
DELIMITER $$
CREATE PROCEDURE sp_dashboard_ultimos_pedidos()
BEGIN
    SELECT
        p.id,
        m.numero                    AS mesa,
        g.nome                      AS garcom,
        p.status,
        p.total + p.taxa_servico    AS total,
        p.data_hora
    FROM Pedido p
    INNER JOIN Mesa   m ON m.id = p.mesa
    INNER JOIN Garcom g ON g.id = p.garcom
    WHERE DATE(p.data_hora) = CURDATE()
      AND p.eh_adicional = 0
    ORDER BY p.data_hora DESC
    LIMIT 8;
END $$
DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_dashboard_resumo $$
CREATE PROCEDURE sp_dashboard_resumo()
BEGIN
    SELECT
        COALESCE(COUNT(*), 0)                AS total_pedidos,
        COALESCE(SUM(total + taxa_servico), 0) AS faturamento -- Soma total + 10%
    FROM Pedido
    WHERE DATE(data_hora) = CURDATE() 
      AND status = 'Finalizado'
      AND eh_adicional = 0; -- IMPORTANTE: Ignora os cards extras da cozinha
END $$

DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_dashboard_mesas $$
CREATE PROCEDURE sp_dashboard_mesas()
BEGIN
    SELECT
        COALESCE(SUM(status = 'Livre'),0)   AS livres,
        COALESCE(SUM(status = 'Ocupado'),0) AS ocupadas,
        COALESCE(COUNT(*),0)                AS total
    FROM Mesa;
END $$

DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_dashboard_faturamento_hora $$
CREATE PROCEDURE sp_dashboard_faturamento_hora()
BEGIN
    SELECT HOUR(data_hora) as hora, SUM(total + taxa_servico) as valor
    FROM Pedido 
    WHERE DATE(data_hora) = CURDATE() 
      AND status = 'Finalizado'
      AND eh_adicional = 0
    GROUP BY HOUR(data_hora) ORDER BY hora;
END $$

DELIMITER ;

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_dashboard_por_categoria $$
CREATE PROCEDURE sp_dashboard_por_categoria()
BEGIN
    SELECT c.nome as categoria, SUM(pi.subtotal) as receita
    FROM Pedido_itens pi
    JOIN Prato p ON p.id = pi.prato
    JOIN categoria c ON c.id = p.categoria
    JOIN Pedido ped ON ped.id = pi.pedido
    WHERE DATE(ped.data_hora) = CURDATE() AND ped.status != 'Cancelado'
    GROUP BY c.id, c.nome ORDER BY receita DESC;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_dashboard_faturamento_mes $$
CREATE PROCEDURE sp_dashboard_faturamento_mes()
BEGIN
    SELECT 
        DATE(data_hora) as data,
        SUM(total) as valor
    FROM Pedido
    WHERE data_hora >= DATE_SUB(CURDATE(), INTERVAL 30 DAY) 
      AND status = 'Finalizado' -- SÓ CONTA O QUE FOI FINALIZADO
    GROUP BY DATE(data_hora)
    ORDER BY data;
END $$
DELIMITER ;

DELIMITER $$
DROP PROCEDURE IF EXISTS sp_comanda_listar_abertas $$
CREATE PROCEDURE sp_comanda_listar_abertas()
BEGIN
    SELECT
        p.id,
        m.numero   AS mesa_numero,
        g.nome     AS garcom_nome,
        p.status,
        p.total,
        p.data_hora,
        p.observacao
    FROM Pedido p
    INNER JOIN Mesa   m ON m.id = p.mesa
    INNER JOIN Garcom g ON g.id = p.garcom
    WHERE p.status IN ('Pendente','Preparando')
    ORDER BY p.data_hora DESC;
END $$

DROP PROCEDURE IF EXISTS sp_comanda_itens $$
CREATE PROCEDURE sp_comanda_itens(IN p_id INT)
BEGIN
    SELECT
        pi.id,
        pr.id   AS prato_id,
        pr.nome AS prato_nome,
        pr.capa_arquivo,
        pi.quantidade,
        pi.preco_unitario,
        pi.subtotal
    FROM Pedido_itens pi
    INNER JOIN Prato pr ON pr.id = pi.prato
    WHERE pi.pedido = p_id;
END $$

DROP PROCEDURE IF EXISTS sp_comanda_adicionar_item $$
CREATE PROCEDURE sp_comanda_adicionar_item(
    IN p_pedido     INT,
    IN p_prato      INT,
    IN p_quantidade INT,
    IN p_preco      DECIMAL(10,2)
)
BEGIN
    -- Adiciona ou incrementa item existente
    IF EXISTS (SELECT 1 FROM Pedido_itens WHERE pedido = p_pedido AND prato = p_prato) THEN
        UPDATE Pedido_itens
        SET quantidade      = quantidade + p_quantidade,
            subtotal        = (quantidade + p_quantidade) * p_preco
        WHERE pedido = p_pedido AND prato = p_prato;
    ELSE
        INSERT INTO Pedido_itens (pedido, prato, quantidade, preco_unitario, subtotal)
        VALUES (p_pedido, p_prato, p_quantidade, p_preco, p_quantidade * p_preco);
    END IF;

    -- Recalcula total do pedido
    UPDATE Pedido
    SET total = (SELECT SUM(subtotal) FROM Pedido_itens WHERE pedido = p_pedido)
    WHERE id = p_pedido;
END $$

DELIMITER ;

ALTER TABLE Pedido ADD COLUMN comanda_fechada TINYINT(1) NOT NULL DEFAULT 0;

DROP PROCEDURE IF EXISTS sp_comanda_listar_abertas;

DELIMITER $$
CREATE PROCEDURE sp_comanda_listar_abertas()
BEGIN
    SELECT
        p.id,
        p.mesa        AS mesa_id,
        p.garcom      AS garcom_id,
        m.numero      AS mesa_numero,
        g.nome        AS garcom_nome,
        p.status,
        p.total,
        p.data_hora,
        p.observacao
    FROM Pedido p
    INNER JOIN Mesa   m ON m.id = p.mesa
    INNER JOIN Garcom g ON g.id = p.garcom
    WHERE p.comanda_fechada = 0
    ORDER BY p.data_hora DESC;
END $$
DELIMITER ;

DROP PROCEDURE IF EXISTS sp_comanda_finalizar;
DELIMITER $$
DROP PROCEDURE IF EXISTS sp_comanda_finalizar $$
CREATE PROCEDURE sp_comanda_finalizar(IN p_id INT)
BEGIN
    -- Calcula 10% sobre o total e fecha a comanda
    UPDATE Pedido 
    SET comanda_fechada = 1, 
        status = 'Finalizado',
        taxa_servico = total * 0.10 
    WHERE id = p_id;
END $$
DELIMITER ;

ALTER TABLE Pedido_itens ADD COLUMN status ENUM('Pendente', 'Preparando', 'Finalizado', 'Cancelado') DEFAULT 'Pendente';

ALTER TABLE Pedido ADD COLUMN eh_adicional TINYINT(1) NOT NULL DEFAULT 0;
ALTER TABLE Pedido ADD COLUMN taxa_servico DECIMAL(10,2) DEFAULT 0.00;